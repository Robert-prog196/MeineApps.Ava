namespace BomberBlast.Models.Entities;

/// <summary>
/// Types of power-ups (original NES Bomberman)
/// </summary>
public enum PowerUpType
{
    /// <summary>+1 simultaneous bomb (max 10) - PERMANENT</summary>
    BombUp,

    /// <summary>+1 explosion range (max 10) - PERMANENT</summary>
    Fire,

    /// <summary>Increased movement speed - LOST ON DEATH</summary>
    Speed,

    /// <summary>Walk through destructible blocks - LOST ON DEATH</summary>
    Wallpass,

    /// <summary>Manual bomb detonation - LOST ON DEATH</summary>
    Detonator,

    /// <summary>Walk through own bombs - LOST ON DEATH</summary>
    Bombpass,

    /// <summary>Immune to explosions - LOST ON DEATH</summary>
    Flamepass,

    /// <summary>35 seconds invincibility - TEMPORARY</summary>
    Mystery
}

public static class PowerUpExtensions
{
    /// <summary>
    /// Check if power-up is permanent (survives death)
    /// </summary>
    public static bool IsPermanent(this PowerUpType type)
    {
        return type switch
        {
            PowerUpType.BombUp => true,
            PowerUpType.Fire => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if power-up is temporary (has duration)
    /// </summary>
    public static bool IsTemporary(this PowerUpType type)
    {
        return type == PowerUpType.Mystery;
    }

    /// <summary>
    /// Get duration in seconds for temporary power-ups
    /// </summary>
    public static float GetDuration(this PowerUpType type)
    {
        return type switch
        {
            PowerUpType.Mystery => 35f,
            _ => 0f
        };
    }

    /// <summary>
    /// Get point value when collected
    /// </summary>
    public static int GetPoints(this PowerUpType type)
    {
        return type switch
        {
            PowerUpType.BombUp => 100,
            PowerUpType.Fire => 100,
            PowerUpType.Speed => 200,
            PowerUpType.Wallpass => 500,
            PowerUpType.Detonator => 500,
            PowerUpType.Bombpass => 500,
            PowerUpType.Flamepass => 500,
            PowerUpType.Mystery => 1000,
            _ => 0
        };
    }

    /// <summary>
    /// Get sprite frame index for this power-up
    /// </summary>
    public static int GetSpriteIndex(this PowerUpType type)
    {
        return (int)type;
    }

    /// <summary>
    /// Get display name key for localization
    /// </summary>
    public static string GetNameKey(this PowerUpType type)
    {
        return $"PowerUp_{type}";
    }
}
