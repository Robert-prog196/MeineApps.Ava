namespace HandwerkerImperium.Models.Enums;

/// <summary>
/// Difficulty levels for orders/contracts.
/// </summary>
public enum OrderDifficulty
{
    /// <summary>Easy - 1 star, forgiving timing</summary>
    Easy = 1,

    /// <summary>Medium - 2 stars, normal timing</summary>
    Medium = 2,

    /// <summary>Hard - 3 stars, precise timing required</summary>
    Hard = 3
}

/// <summary>
/// Extension methods for OrderDifficulty.
/// </summary>
public static class OrderDifficultyExtensions
{
    /// <summary>
    /// Gets the star display string.
    /// </summary>
    public static string GetStars(this OrderDifficulty difficulty) => difficulty switch
    {
        OrderDifficulty.Easy => "\u2b50",
        OrderDifficulty.Medium => "\u2b50\u2b50",
        OrderDifficulty.Hard => "\u2b50\u2b50\u2b50",
        _ => "\u2b50"
    };

    /// <summary>
    /// Gets the reward multiplier for this difficulty.
    /// </summary>
    public static decimal GetRewardMultiplier(this OrderDifficulty difficulty) => difficulty switch
    {
        OrderDifficulty.Easy => 1.0m,
        OrderDifficulty.Medium => 1.5m,
        OrderDifficulty.Hard => 3.5m,  // Increased from 2.5 to make Hard more rewarding
        _ => 1.0m
    };

    /// <summary>
    /// Gets the XP multiplier for this difficulty.
    /// </summary>
    public static decimal GetXpMultiplier(this OrderDifficulty difficulty) => difficulty switch
    {
        OrderDifficulty.Easy => 1.0m,
        OrderDifficulty.Medium => 1.75m,
        OrderDifficulty.Hard => 3.0m,
        _ => 1.0m
    };

    /// <summary>
    /// Gets the size of the "perfect" zone in the timing bar (0.0 - 1.0).
    /// Smaller = harder.
    /// </summary>
    public static double GetPerfectZoneSize(this OrderDifficulty difficulty) => difficulty switch
    {
        OrderDifficulty.Easy => 0.25,    // 25% of bar
        OrderDifficulty.Medium => 0.15,  // 15% of bar
        OrderDifficulty.Hard => 0.12,    // 12% of bar (was 8% - too hard)
        _ => 0.15
    };

    /// <summary>
    /// Gets the timing bar speed multiplier.
    /// Higher = faster = harder.
    /// </summary>
    public static double GetSpeedMultiplier(this OrderDifficulty difficulty) => difficulty switch
    {
        OrderDifficulty.Easy => 0.8,
        OrderDifficulty.Medium => 1.0,
        OrderDifficulty.Hard => 1.4,
        _ => 1.0
    };
}
