using Avalonia.Threading;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.Models.Events;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Manages the game loop timer for idle earnings.
/// Uses Avalonia DispatcherTimer instead of MAUI IDispatcherTimer.
/// Auto-saves every 30 seconds.
/// </summary>
public class GameLoopService : IGameLoopService, IDisposable
{
    private readonly IGameStateService _gameStateService;
    private readonly ISaveGameService _saveGameService;
    private DispatcherTimer? _timer;
    private DateTime _sessionStart;
    private bool _isPaused;
    private bool _disposed;
    private int _tickCount;

    private const int AutoSaveIntervalTicks = 30;

    public bool IsRunning => _timer?.IsEnabled ?? false;
    public TimeSpan SessionDuration => DateTime.UtcNow - _sessionStart;

    public event EventHandler<GameTickEventArgs>? OnTick;

    public GameLoopService(IGameStateService gameStateService, ISaveGameService saveGameService)
    {
        _gameStateService = gameStateService;
        _saveGameService = saveGameService;
    }

    public void Start()
    {
        if (_timer != null && _timer.IsEnabled)
            return;

        _sessionStart = DateTime.UtcNow;
        _isPaused = false;
        _tickCount = 0;

        // Create Avalonia DispatcherTimer on UI thread
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer == null) return;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _timer = null;

        // Update total play time
        _gameStateService.State.TotalPlayTimeSeconds += (long)SessionDuration.TotalSeconds;
        _gameStateService.State.LastPlayedAt = DateTime.UtcNow;

        // Final save on stop
        _saveGameService.SaveAsync().FireAndForget();
    }

    public void Pause()
    {
        _isPaused = true;
        _timer?.Stop();

        // Save on pause (app going to background)
        _gameStateService.State.LastPlayedAt = DateTime.UtcNow;
        _saveGameService.SaveAsync().FireAndForget();
    }

    public void Resume()
    {
        _isPaused = false;
        _timer?.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isPaused || !_gameStateService.IsInitialized)
            return;

        // Calculate earnings this tick
        decimal earnings = _gameStateService.State.TotalIncomePerSecond;

        if (earnings > 0)
        {
            _gameStateService.AddMoney(earnings);
        }

        // Update last played time
        _gameStateService.State.LastPlayedAt = DateTime.UtcNow;

        // Auto-save periodically
        _tickCount++;
        if (_tickCount >= AutoSaveIntervalTicks)
        {
            _tickCount = 0;
            _saveGameService.SaveAsync().FireAndForget();
        }

        // Fire tick event
        OnTick?.Invoke(this, new GameTickEventArgs(
            earnings,
            _gameStateService.State.Money,
            SessionDuration));
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
