using BomberBlast.Models.Entities;

namespace BomberBlast.Models.Levels;

/// <summary>
/// Represents a game level configuration
/// </summary>
public class Level
{
    /// <summary>Level number (1-50)</summary>
    public int Number { get; set; }

    /// <summary>Level name/title</summary>
    public string Name { get; set; } = "";

    /// <summary>Time limit in seconds (default 200)</summary>
    public int TimeLimit { get; set; } = 200;

    /// <summary>Block density (0.0-1.0)</summary>
    public float BlockDensity { get; set; } = 0.5f;

    /// <summary>Enemy spawns</summary>
    public List<EnemySpawn> Enemies { get; set; } = new();

    /// <summary>Power-ups hidden in blocks</summary>
    public List<PowerUpPlacement> PowerUps { get; set; } = new();

    /// <summary>Fixed block positions (null = random)</summary>
    public List<(int x, int y)>? FixedBlocks { get; set; }

    /// <summary>Exit position (null = random under block)</summary>
    public (int x, int y)? ExitPosition { get; set; }

    /// <summary>Random seed for reproducible generation</summary>
    public int? Seed { get; set; }

    /// <summary>Whether this is a bonus level</summary>
    public bool IsBonusLevel { get; set; }

    /// <summary>Background music track</summary>
    public string MusicTrack { get; set; } = "gameplay";
}

/// <summary>
/// Enemy spawn configuration
/// </summary>
public class EnemySpawn
{
    public EnemyType Type { get; set; }
    public int Count { get; set; } = 1;
    public int? X { get; set; }
    public int? Y { get; set; }
}

/// <summary>
/// Power-up placement configuration
/// </summary>
public class PowerUpPlacement
{
    public PowerUpType Type { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
}
