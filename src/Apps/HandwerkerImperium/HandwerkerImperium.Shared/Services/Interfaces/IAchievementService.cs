using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Manages achievements and tracks progress.
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Event fired when an achievement is unlocked.
    /// </summary>
    event EventHandler<Achievement>? AchievementUnlocked;

    /// <summary>
    /// Gets all achievements with their current progress.
    /// </summary>
    List<Achievement> GetAllAchievements();

    /// <summary>
    /// Gets all unlocked achievements.
    /// </summary>
    List<Achievement> GetUnlockedAchievements();

    /// <summary>
    /// Gets the count of unlocked achievements.
    /// </summary>
    int UnlockedCount { get; }

    /// <summary>
    /// Gets the total count of achievements.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Checks and updates achievement progress.
    /// Call this after relevant game state changes.
    /// </summary>
    void CheckAchievements();

    /// <summary>
    /// Gets a specific achievement by ID.
    /// </summary>
    Achievement? GetAchievement(string id);
}
