using HandwerkerImperium.Models.Events;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Calculates earnings while the player was offline.
/// </summary>
public interface IOfflineProgressService
{
    /// <summary>
    /// Fired when offline earnings are calculated.
    /// </summary>
    event EventHandler<OfflineEarningsEventArgs>? OfflineEarningsCalculated;

    /// <summary>
    /// Calculates and applies offline progress.
    /// Returns the earnings amount.
    /// </summary>
    decimal CalculateOfflineProgress();

    /// <summary>
    /// Gets the maximum offline duration (depends on premium status).
    /// </summary>
    TimeSpan GetMaxOfflineDuration();

    /// <summary>
    /// Checks how long the player was offline.
    /// </summary>
    TimeSpan GetOfflineDuration();
}
