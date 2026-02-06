using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.Services.Interfaces;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerImperium.ViewModels;

/// <summary>
/// ViewModel for the Pipe Puzzle mini-game.
/// Player must rotate pipe segments to connect water from source to drain.
/// </summary>
public partial class PipePuzzleViewModel : ObservableObject, IDisposable
{
    private readonly IGameStateService _gameStateService;
    private readonly IAudioService _audioService;
    private readonly IRewardedAdService _rewardedAdService;
    private readonly ILocalizationService _localizationService;
    private DispatcherTimer? _timer;
    private bool _disposed;
    private bool _isEnding;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public event Action<string>? NavigationRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private string _orderId = string.Empty;

    [ObservableProperty]
    private OrderDifficulty _difficulty = OrderDifficulty.Medium;

    [ObservableProperty]
    private ObservableCollection<PipeTile> _tiles = [];

    [ObservableProperty]
    private int _gridSize = 4;

    [ObservableProperty]
    private int _movesCount;

    [ObservableProperty]
    private int _timeRemaining;

    [ObservableProperty]
    private int _maxTime = 60;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPuzzleSolved;

    [ObservableProperty]
    private bool _isResultShown;

    [ObservableProperty]
    private MiniGameRating _result;

    [ObservableProperty]
    private string _resultText = "";

    [ObservableProperty]
    private string _resultEmoji = "";

    [ObservableProperty]
    private decimal _rewardAmount;

    [ObservableProperty]
    private int _xpAmount;

    [ObservableProperty]
    private bool _canWatchAd;

    [ObservableProperty]
    private bool _adWatched;

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the difficulty as star display string.
    /// </summary>
    public string DifficultyStars => Difficulty switch
    {
        OrderDifficulty.Easy => "★☆☆",
        OrderDifficulty.Medium => "★★☆",
        OrderDifficulty.Hard => "★★★",
        _ => "★☆☆"
    };

    /// <summary>
    /// Width of the puzzle grid in pixels for WrapPanel constraint.
    /// Each tile is 60px + 4px margin = 64px.
    /// </summary>
    public double PuzzleGridWidth => GridSize * 64;

    partial void OnDifficultyChanged(OrderDifficulty value) => OnPropertyChanged(nameof(DifficultyStars));
    partial void OnGridSizeChanged(int value) => OnPropertyChanged(nameof(PuzzleGridWidth));

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public PipePuzzleViewModel(
        IGameStateService gameStateService,
        IAudioService audioService,
        IRewardedAdService rewardedAdService,
        ILocalizationService localizationService)
    {
        _gameStateService = gameStateService;
        _audioService = audioService;
        _rewardedAdService = rewardedAdService;
        _localizationService = localizationService;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION (replaces IQueryAttributable)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialize the game with an order ID.
    /// </summary>
    public void SetOrderId(string orderId)
    {
        OrderId = orderId;

        var activeOrder = _gameStateService.GetActiveOrder();
        if (activeOrder != null)
        {
            Difficulty = activeOrder.Difficulty;
        }

        InitializePuzzle();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GAME LOGIC
    // ═══════════════════════════════════════════════════════════════════════

    private void InitializePuzzle()
    {
        // Set grid size and time based on difficulty
        (GridSize, MaxTime) = Difficulty switch
        {
            OrderDifficulty.Easy => (3, 45),
            OrderDifficulty.Medium => (4, 60),
            OrderDifficulty.Hard => (5, 90),
            _ => (4, 60)
        };

        TimeRemaining = MaxTime;
        MovesCount = 0;
        IsPlaying = false;
        IsPuzzleSolved = false;
        IsResultShown = false;

        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        Tiles.Clear();
        var random = new Random();

        // Generate a solvable puzzle
        // First create the solution path, then add random pipes

        // Create a simple path from left to right
        var path = GeneratePath();

        // Fill the grid with pipes
        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                var tile = new PipeTile
                {
                    Row = row,
                    Column = col,
                    Index = row * GridSize + col
                };

                // Check if this cell is on the path
                var pathCell = path.FirstOrDefault(p => p.Row == row && p.Col == col);
                if (pathCell != null)
                {
                    tile.PipeType = pathCell.Type;
                    tile.IsPartOfSolution = true;
                    // Randomize initial rotation (player needs to fix it)
                    tile.Rotation = random.Next(4) * 90;
                }
                else
                {
                    // Add random pipe that's not on the critical path
                    tile.PipeType = GetRandomPipeType(random);
                    tile.Rotation = random.Next(4) * 90;
                    tile.IsPartOfSolution = false;
                }

                // Mark source and drain
                if (col == 0 && row == GridSize / 2)
                {
                    tile.IsSource = true;
                }
                else if (col == GridSize - 1 && row == GridSize / 2)
                {
                    tile.IsDrain = true;
                }

                Tiles.Add(tile);
            }
        }
    }

    private List<PathCell> GeneratePath()
    {
        var path = new List<PathCell>();
        var random = new Random();
        int currentRow = GridSize / 2;
        int currentCol = 0;

        // Start with source connector (horizontal going right)
        path.Add(new PathCell(currentRow, currentCol, PipeType.Straight, Direction.Right));
        currentCol++;

        // Generate path to the right side
        while (currentCol < GridSize - 1)
        {
            var directions = new List<Direction> { Direction.Right };

            // Allow going up/down occasionally
            if (currentRow > 0 && random.Next(3) == 0)
                directions.Add(Direction.Up);
            if (currentRow < GridSize - 1 && random.Next(3) == 0)
                directions.Add(Direction.Down);

            var dir = directions[random.Next(directions.Count)];

            if (dir == Direction.Right)
            {
                // Determine pipe type based on previous direction
                var prevCell = path.Last();
                PipeType type;

                if (prevCell.ExitDirection == Direction.Up || prevCell.ExitDirection == Direction.Down)
                {
                    type = PipeType.Corner;
                }
                else
                {
                    type = PipeType.Straight;
                }

                path.Add(new PathCell(currentRow, currentCol, type, Direction.Right));
                currentCol++;
            }
            else if (dir == Direction.Up && currentRow > 0)
            {
                path.Add(new PathCell(currentRow, currentCol, PipeType.Corner, Direction.Up));
                currentRow--;
                // Add vertical piece
                if (currentCol < GridSize - 1)
                {
                    path.Add(new PathCell(currentRow, currentCol, PipeType.Corner, Direction.Right));
                    currentCol++;
                }
            }
            else if (dir == Direction.Down && currentRow < GridSize - 1)
            {
                path.Add(new PathCell(currentRow, currentCol, PipeType.Corner, Direction.Down));
                currentRow++;
                if (currentCol < GridSize - 1)
                {
                    path.Add(new PathCell(currentRow, currentCol, PipeType.Corner, Direction.Right));
                    currentCol++;
                }
            }
        }

        // End with drain connector
        path.Add(new PathCell(currentRow, currentCol, PipeType.Straight, Direction.Right));

        return path;
    }

    private static PipeType GetRandomPipeType(Random random)
    {
        return random.Next(4) switch
        {
            0 => PipeType.Straight,
            1 => PipeType.Corner,
            2 => PipeType.TJunction,
            _ => PipeType.Cross
        };
    }

    [RelayCommand]
    private async Task StartGameAsync()
    {
        if (IsPlaying) return;

        IsPlaying = true;
        IsResultShown = false;
        _isEnding = false;
        await _audioService.PlaySoundAsync(GameSound.ButtonTap);

        // Start countdown timer using Avalonia DispatcherTimer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (!IsPlaying || _isEnding) return;

        TimeRemaining--;

        if (TimeRemaining <= 0)
        {
            await EndGameAsync(false);
        }
    }

    [RelayCommand]
    private async Task RotateTileAsync(PipeTile? tile)
    {
        if (tile == null || !IsPlaying || IsResultShown) return;

        // Rotate by 90 degrees
        tile.Rotation = (tile.Rotation + 90) % 360;
        MovesCount++;

        await _audioService.PlaySoundAsync(GameSound.ButtonTap);

        // Check if puzzle is solved
        if (CheckIfSolved())
        {
            IsPuzzleSolved = true;
            await EndGameAsync(true);
        }
    }

    private bool CheckIfSolved()
    {
        // Check if water can flow from source to drain
        // Water enters the source tile from the Left side (outside the grid)
        var visited = new HashSet<int>();
        return TracePath(GridSize / 2, 0, Direction.Left, visited);
    }

    private bool TracePath(int row, int col, Direction fromDirection, HashSet<int> visited)
    {
        // Out of bounds
        if (row < 0 || row >= GridSize || col < 0 || col >= GridSize)
            return false;

        int index = row * GridSize + col;

        // Already visited
        if (visited.Contains(index))
            return false;

        visited.Add(index);

        var tile = Tiles[index];

        // Check if pipe connects from the incoming direction
        if (!tile.ConnectsFrom(fromDirection))
            return false;

        // Reached the drain!
        if (tile.IsDrain)
            return true;

        // Get the exit directions from this pipe
        var exits = tile.GetExitDirections(fromDirection);

        foreach (var exit in exits)
        {
            int nextRow = row;
            int nextCol = col;
            Direction nextFrom;

            switch (exit)
            {
                case Direction.Up:
                    nextRow--;
                    nextFrom = Direction.Down;
                    break;
                case Direction.Down:
                    nextRow++;
                    nextFrom = Direction.Up;
                    break;
                case Direction.Left:
                    nextCol--;
                    nextFrom = Direction.Right;
                    break;
                case Direction.Right:
                    nextCol++;
                    nextFrom = Direction.Left;
                    break;
                default:
                    continue;
            }

            if (TracePath(nextRow, nextCol, nextFrom, visited))
                return true;
        }

        return false;
    }

    private async Task EndGameAsync(bool solved)
    {
        if (_isEnding) return;
        _isEnding = true;

        IsPlaying = false;
        _timer?.Stop();

        // Calculate rating based on performance
        if (solved)
        {
            double timeRatio = (double)TimeRemaining / MaxTime;
            double moveEfficiency = GridSize * GridSize / (double)Math.Max(MovesCount, 1);

            if (timeRatio > 0.6 && moveEfficiency > 0.5)
                Result = MiniGameRating.Perfect;
            else if (timeRatio > 0.3 && moveEfficiency > 0.3)
                Result = MiniGameRating.Good;
            else
                Result = MiniGameRating.Ok;
        }
        else
        {
            Result = MiniGameRating.Miss;
        }

        // Record result
        _gameStateService.RecordMiniGameResult(Result);

        // Play sound
        var sound = Result switch
        {
            MiniGameRating.Perfect => GameSound.Perfect,
            MiniGameRating.Good => GameSound.Good,
            MiniGameRating.Ok => GameSound.ButtonTap,
            _ => GameSound.Miss
        };
        await _audioService.PlaySoundAsync(sound);

        // Calculate rewards
        var order = _gameStateService.GetActiveOrder();
        if (order != null)
        {
            decimal baseReward = order.BaseReward / order.Tasks.Count;
            RewardAmount = baseReward * Result.GetRewardPercentage();

            int baseXp = order.BaseXp / order.Tasks.Count;
            XpAmount = (int)(baseXp * Result.GetXpPercentage());
        }

        // Set result display
        ResultText = _localizationService.GetString(Result.GetLocalizationKey());
        ResultEmoji = Result switch
        {
            MiniGameRating.Perfect => "⭐⭐⭐",
            MiniGameRating.Good => "⭐⭐",
            MiniGameRating.Ok => "⭐",
            _ => "💨"
        };

        IsResultShown = true;
        AdWatched = false;
        CanWatchAd = _rewardedAdService.IsAvailable;
    }

    [RelayCommand]
    private async Task WatchAdAsync()
    {
        if (!CanWatchAd || AdWatched) return;

        var success = await _rewardedAdService.ShowAdAsync();
        if (success)
        {
            RewardAmount *= 2;
            XpAmount *= 2;
            AdWatched = true;
            CanWatchAd = false;

            await _audioService.PlaySoundAsync(GameSound.MoneyEarned);
        }
    }

    [RelayCommand]
    private void Continue()
    {
        var order = _gameStateService.GetActiveOrder();
        if (order == null)
        {
            NavigationRequested?.Invoke("../..");
            return;
        }

        if (order.IsCompleted)
        {
            _gameStateService.CompleteActiveOrder();
            NavigationRequested?.Invoke("../..");
        }
        else
        {
            var nextTask = order.CurrentTask;
            if (nextTask != null)
            {
                NavigationRequested?.Invoke($"../{nextTask.GameType.GetRoute()}?orderId={order.Id}");
            }
            else
            {
                NavigationRequested?.Invoke("../..");
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _timer?.Stop();
        IsPlaying = false;

        _gameStateService.CancelActiveOrder();
        NavigationRequested?.Invoke("../..");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DISPOSAL
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        _timer?.Stop();
        if (_timer != null)
        {
            _timer.Tick -= OnTimerTick;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SUPPORTING TYPES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Types of pipe segments.
/// </summary>
public enum PipeType
{
    Straight,   // Connects two opposite sides
    Corner,     // Connects two adjacent sides (L-shape)
    TJunction,  // Connects three sides (T-shape)
    Cross       // Connects all four sides
}

/// <summary>
/// Direction for pipe connections.
/// </summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Represents a single tile in the pipe puzzle.
/// </summary>
public partial class PipeTile : ObservableObject
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int Index { get; set; }
    public PipeType PipeType { get; set; }
    public bool IsSource { get; set; }
    public bool IsDrain { get; set; }
    public bool IsPartOfSolution { get; set; }

    [ObservableProperty]
    private int _rotation; // 0, 90, 180, 270

    [ObservableProperty]
    private bool _isConnected; // For visual feedback when water flows through

    // Notify pipe opening properties when rotation changes
    partial void OnRotationChanged(int value)
    {
        OnPropertyChanged(nameof(HasTopOpening));
        OnPropertyChanged(nameof(HasBottomOpening));
        OnPropertyChanged(nameof(HasLeftOpening));
        OnPropertyChanged(nameof(HasRightOpening));
    }

    /// <summary>Whether the pipe has an opening on the top side at current rotation.</summary>
    public bool HasTopOpening => GetOpenings().Contains(Direction.Up);

    /// <summary>Whether the pipe has an opening on the bottom side at current rotation.</summary>
    public bool HasBottomOpening => GetOpenings().Contains(Direction.Down);

    /// <summary>Whether the pipe has an opening on the left side at current rotation.</summary>
    public bool HasLeftOpening => GetOpenings().Contains(Direction.Left);

    /// <summary>Whether the pipe has an opening on the right side at current rotation.</summary>
    public bool HasRightOpening => GetOpenings().Contains(Direction.Right);

    /// <summary>
    /// Checks if this pipe connects from the given direction.
    /// </summary>
    public bool ConnectsFrom(Direction fromDirection)
    {
        var openings = GetOpenings();
        return openings.Contains(fromDirection);
    }

    /// <summary>
    /// Gets all exit directions from this pipe when entering from the given direction.
    /// </summary>
    public List<Direction> GetExitDirections(Direction fromDirection)
    {
        var openings = GetOpenings();
        return openings.Where(d => d != fromDirection).ToList();
    }

    /// <summary>
    /// Gets the openings of this pipe based on type and rotation.
    /// </summary>
    private List<Direction> GetOpenings()
    {
        // Base openings for each pipe type (at 0 rotation)
        var baseOpenings = PipeType switch
        {
            PipeType.Straight => new[] { Direction.Left, Direction.Right },
            PipeType.Corner => new[] { Direction.Right, Direction.Down },
            PipeType.TJunction => new[] { Direction.Right, Direction.Down, Direction.Left },
            PipeType.Cross => new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right },
            _ => Array.Empty<Direction>()
        };

        // Rotate based on current rotation
        return baseOpenings.Select(d => RotateDirection(d, Rotation)).ToList();
    }

    private static Direction RotateDirection(Direction dir, int rotation)
    {
        int steps = rotation / 90;
        for (int i = 0; i < steps; i++)
        {
            dir = dir switch
            {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Left,
                Direction.Left => Direction.Up,
                _ => dir
            };
        }
        return dir;
    }
}

/// <summary>
/// Helper class for path generation.
/// </summary>
internal record PathCell(int Row, int Col, PipeType Type, Direction ExitDirection);
