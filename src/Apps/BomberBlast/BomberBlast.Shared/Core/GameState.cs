namespace BomberBlast.Core;

/// <summary>
/// Represents the current state of the game
/// </summary>
public enum GameState
{
    /// <summary>Game not started, showing menu</summary>
    Menu,

    /// <summary>Level loading/countdown</summary>
    Starting,

    /// <summary>Active gameplay</summary>
    Playing,

    /// <summary>Game is paused</summary>
    Paused,

    /// <summary>Player died, showing death animation</summary>
    PlayerDied,

    /// <summary>Level completed, showing victory animation</summary>
    LevelComplete,

    /// <summary>All lives lost, game over</summary>
    GameOver,

    /// <summary>All 50 levels completed</summary>
    Victory
}
