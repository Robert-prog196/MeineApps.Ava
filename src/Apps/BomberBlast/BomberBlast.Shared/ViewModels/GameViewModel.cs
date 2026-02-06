using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomberBlast.Core;
using SkiaSharp;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel for the game page.
/// Wraps GameEngine, manages the game loop via render-driven updates, and owns SKCanvas rendering.
/// </summary>
public partial class GameViewModel : ObservableObject, IDisposable
{
    private const float MAX_DELTA_TIME = 0.1f;

    private readonly GameEngine _gameEngine;
    private DateTime _lastUpdate;
    private bool _isInitialized;
    private bool _disposed;
    private bool _isGameLoopRunning;

    private string _mode = "story";
    private int _level = 1;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event to request navigation. Parameter is the route string.
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Event to request the canvas to invalidate (repaint).
    /// </summary>
    public event Action? InvalidateCanvasRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isPaused;

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current game state from the engine.
    /// </summary>
    public GameState State => _gameEngine.State;

    /// <summary>
    /// The game engine instance (for views that need direct access to render).
    /// </summary>
    public GameEngine Engine => _gameEngine;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public GameViewModel(GameEngine gameEngine)
    {
        _gameEngine = gameEngine;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set the game mode and level parameters before starting.
    /// Resets initialization flag so the game engine reinitializes on next OnAppearingAsync.
    /// </summary>
    public void SetParameters(string mode, int level)
    {
        _mode = mode;
        _level = level;
        _isInitialized = false;
    }

    /// <summary>
    /// Called when the view appears. Initializes the game and starts the loop.
    /// </summary>
    public async Task OnAppearingAsync()
    {
        // Subscribe to game events
        _gameEngine.OnGameOver += HandleGameOver;
        _gameEngine.OnLevelComplete += HandleLevelComplete;

        if (!_isInitialized)
        {
            _isInitialized = true;
            try
            {
                await InitializeGameAsync();
            }
            catch
            {
                NavigationRequested?.Invoke("..");
                return;
            }
        }

        IsLoading = false;
        _lastUpdate = DateTime.Now;
        StartGameLoop();
    }

    /// <summary>
    /// Called when the view disappears. Stops the game loop and pauses.
    /// </summary>
    public void OnDisappearing()
    {
        StopGameLoop();
        _gameEngine.Pause();

        // Unsubscribe from game events to prevent memory leaks
        _gameEngine.OnGameOver -= HandleGameOver;
        _gameEngine.OnLevelComplete -= HandleLevelComplete;
    }

    private async Task InitializeGameAsync()
    {
        switch (_mode.ToLower())
        {
            case "arcade":
                await _gameEngine.StartArcadeModeAsync();
                break;

            case "quick":
                var random = new Random();
                int randomLevel = random.Next(1, 11); // First 10 levels for quick play
                await _gameEngine.StartStoryModeAsync(randomLevel);
                break;

            case "story":
            default:
                await _gameEngine.StartStoryModeAsync(_level);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GAME LOOP (render-driven)
    // ═══════════════════════════════════════════════════════════════════════

    private void StartGameLoop()
    {
        _isGameLoopRunning = true;
        // Kick off the first frame
        InvalidateCanvasRequested?.Invoke();
    }

    private void StopGameLoop()
    {
        _isGameLoopRunning = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RENDERING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by the SKCanvasView to paint the game.
    /// Drives both update and render in a single frame - each paint triggers the next.
    /// </summary>
    public void OnPaintSurface(SKCanvas canvas, int width, int height)
    {
        // Update game state if the loop is running
        if (_isGameLoopRunning)
        {
            var now = DateTime.Now;
            float deltaTime = (float)(now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            // Clamp delta time to prevent large jumps
            deltaTime = Math.Min(deltaTime, MAX_DELTA_TIME);

            _gameEngine.Update(deltaTime);
        }

        // Always render current state
        _gameEngine.Render(canvas, width, height);

        // Schedule next frame (render-driven loop)
        if (_isGameLoopRunning)
        {
            Dispatcher.UIThread.Post(() => InvalidateCanvasRequested?.Invoke(),
                DispatcherPriority.Render);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TOUCH INPUT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handle touch/pointer press.
    /// </summary>
    public void OnPointerPressed(float x, float y, float screenWidth, float screenHeight)
    {
        // Handle game over state - return to menu on tap
        if (_gameEngine.State == GameState.GameOver)
        {
            Dispatcher.UIThread.Post(() => NavigationRequested?.Invoke(".."));
            return;
        }

        // Handle paused state - resume on tap
        if (_gameEngine.State == GameState.Paused)
        {
            _gameEngine.Resume();
            IsPaused = false;
            return;
        }

        // Forward touch to input manager via game engine
        if (_gameEngine.State == GameState.Playing)
        {
            _gameEngine.OnTouchStart(x, y, screenWidth, screenHeight);
        }
    }

    /// <summary>
    /// Handle touch/pointer move.
    /// </summary>
    public void OnPointerMoved(float x, float y)
    {
        if (_gameEngine.State == GameState.Playing)
        {
            _gameEngine.OnTouchMove(x, y);
        }
    }

    /// <summary>
    /// Handle touch/pointer release.
    /// </summary>
    public void OnPointerReleased()
    {
        if (_gameEngine.State == GameState.Playing)
        {
            _gameEngine.OnTouchEnd();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // KEYBOARD INPUT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Forward keyboard key-down to the game engine.
    /// </summary>
    public void OnKeyDown(Key key)
    {
        // Escape toggles pause
        if (key == Key.Escape)
        {
            if (_gameEngine.State == GameState.Playing)
                Pause();
            else if (_gameEngine.State == GameState.Paused)
                Resume();
            return;
        }

        if (_gameEngine.State == GameState.Playing)
        {
            _gameEngine.OnKeyDown(key);
        }
    }

    /// <summary>
    /// Forward keyboard key-up to the game engine.
    /// </summary>
    public void OnKeyUp(Key key)
    {
        _gameEngine.OnKeyUp(key);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void Pause()
    {
        if (_gameEngine.State == GameState.Playing)
        {
            _gameEngine.Pause();
            IsPaused = true;
        }
    }

    [RelayCommand]
    private void Resume()
    {
        _gameEngine.Resume();
        IsPaused = false;
    }

    [RelayCommand]
    private async Task Restart()
    {
        _isInitialized = false;
        IsPaused = false;
        await InitializeGameAsync();
        _isInitialized = true;
        _lastUpdate = DateTime.Now;
    }

    [RelayCommand]
    private void Settings()
    {
        _gameEngine.Pause();
        IsPaused = false;
        NavigationRequested?.Invoke("Settings");
    }

    [RelayCommand]
    private void QuitToMenu()
    {
        StopGameLoop();
        IsPaused = false;
        NavigationRequested?.Invoke("..");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GAME EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private async void HandleGameOver()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(2000); // Show game over briefly

            var score = _gameEngine.Score;
            var level = _gameEngine.IsArcadeMode ? _gameEngine.ArcadeWave : _gameEngine.CurrentLevel;
            var isHighScore = _gameEngine.IsCurrentScoreHighScore;
            var mode = _gameEngine.IsArcadeMode ? "arcade" : "story";

            NavigationRequested?.Invoke(
                $"GameOver?score={score}&level={level}&highscore={isHighScore}&mode={mode}");
        });
    }

    private async void HandleLevelComplete()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(3000); // Show level complete for 3 seconds

            if (_gameEngine.CurrentLevel >= 50 && !_gameEngine.IsArcadeMode)
            {
                // Victory!
                NavigationRequested?.Invoke("..");
            }
            else
            {
                // Next level
                await _gameEngine.NextLevelAsync();
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DISPOSAL
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        StopGameLoop();

        _gameEngine.OnGameOver -= HandleGameOver;
        _gameEngine.OnLevelComplete -= HandleLevelComplete;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
