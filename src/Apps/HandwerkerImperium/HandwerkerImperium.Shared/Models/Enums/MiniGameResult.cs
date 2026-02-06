namespace HandwerkerImperium.Models.Enums;

/// <summary>
/// Result rating for mini-game performance.
/// </summary>
public enum MiniGameRating
{
    /// <summary>Missed the target zone entirely</summary>
    Miss = 0,

    /// <summary>Hit the outer OK zone</summary>
    Ok = 1,

    /// <summary>Hit the inner Good zone</summary>
    Good = 2,

    /// <summary>Hit the center Perfect zone</summary>
    Perfect = 3
}

/// <summary>
/// Extension methods for MiniGameRating.
/// </summary>
public static class MiniGameRatingExtensions
{
    /// <summary>
    /// Gets the reward percentage for this rating.
    /// </summary>
    public static decimal GetRewardPercentage(this MiniGameRating rating) => rating switch
    {
        MiniGameRating.Miss => 0.50m,    // 50% of base reward
        MiniGameRating.Ok => 0.75m,      // 75% of base reward
        MiniGameRating.Good => 1.00m,    // 100% of base reward
        MiniGameRating.Perfect => 1.50m, // 150% of base reward (bonus!)
        _ => 1.0m
    };

    /// <summary>
    /// Gets the XP percentage for this rating.
    /// </summary>
    public static decimal GetXpPercentage(this MiniGameRating rating) => rating switch
    {
        MiniGameRating.Miss => 0.25m,    // 25% XP
        MiniGameRating.Ok => 0.50m,      // 50% XP
        MiniGameRating.Good => 1.00m,    // 100% XP
        MiniGameRating.Perfect => 1.50m, // 150% XP (increased from 125% for better reward)
        _ => 1.0m
    };

    /// <summary>
    /// Gets the localization key for this rating.
    /// </summary>
    public static string GetLocalizationKey(this MiniGameRating rating) => rating switch
    {
        MiniGameRating.Miss => "Miss",
        MiniGameRating.Ok => "Ok",
        MiniGameRating.Good => "Good",
        MiniGameRating.Perfect => "Perfect",
        _ => "Ok"
    };

    /// <summary>
    /// Gets the display color key for this rating.
    /// </summary>
    public static string GetColorKey(this MiniGameRating rating) => rating switch
    {
        MiniGameRating.Miss => "ResultMissColor",
        MiniGameRating.Ok => "ResultOkColor",
        MiniGameRating.Good => "ResultGoodColor",
        MiniGameRating.Perfect => "ResultPerfectColor",
        _ => "TextPrimaryColor"
    };
}
