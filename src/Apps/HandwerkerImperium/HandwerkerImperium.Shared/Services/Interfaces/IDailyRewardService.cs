using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Manages daily login rewards with a 7-day cycle and streak tracking.
/// </summary>
public interface IDailyRewardService
{
    /// <summary>
    /// Gets the current day in the reward cycle (1-7).
    /// </summary>
    int CurrentDay { get; }

    /// <summary>
    /// Gets the current streak (consecutive days logged in).
    /// </summary>
    int CurrentStreak { get; }

    /// <summary>
    /// Gets whether today's reward is available to claim.
    /// </summary>
    bool IsRewardAvailable { get; }

    /// <summary>
    /// Gets today's reward (or null if already claimed).
    /// </summary>
    DailyReward? TodaysReward { get; }

    /// <summary>
    /// Gets all rewards in the current cycle with their status.
    /// </summary>
    List<DailyReward> GetRewardCycle();

    /// <summary>
    /// Claims today's reward and applies it to the game state.
    /// </summary>
    /// <returns>The claimed reward, or null if not available.</returns>
    DailyReward? ClaimReward();

    /// <summary>
    /// Checks if the streak was broken (missed a day).
    /// </summary>
    bool WasStreakBroken();

    /// <summary>
    /// Gets time until next reward becomes available.
    /// </summary>
    TimeSpan TimeUntilNextReward { get; }
}
