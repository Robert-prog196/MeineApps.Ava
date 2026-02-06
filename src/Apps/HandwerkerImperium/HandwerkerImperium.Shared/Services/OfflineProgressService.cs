using HandwerkerImperium.Models.Events;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Calculates earnings while the player was offline.
/// </summary>
public class OfflineProgressService : IOfflineProgressService
{
    private readonly IGameStateService _gameStateService;

    public event EventHandler<OfflineEarningsEventArgs>? OfflineEarningsCalculated;

    public OfflineProgressService(IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
    }

    public decimal CalculateOfflineProgress()
    {
        var state = _gameStateService.State;

        // Calculate time offline
        var offlineDuration = GetOfflineDuration();
        if (offlineDuration.TotalSeconds < 60) // Less than 1 minute, no rewards
        {
            return 0;
        }

        // Cap offline duration
        var maxDuration = GetMaxOfflineDuration();
        bool wasCapped = offlineDuration > maxDuration;
        var effectiveDuration = wasCapped ? maxDuration : offlineDuration;

        // Calculate earnings (income per second x seconds offline)
        decimal earnings = state.TotalIncomePerSecond * (decimal)effectiveDuration.TotalSeconds;

        // Fire event
        OfflineEarningsCalculated?.Invoke(this, new OfflineEarningsEventArgs(
            earnings,
            effectiveDuration,
            wasCapped));

        return earnings;
    }

    public TimeSpan GetMaxOfflineDuration()
    {
        int maxHours = _gameStateService.State.MaxOfflineHours;
        return TimeSpan.FromHours(maxHours);
    }

    public TimeSpan GetOfflineDuration()
    {
        var lastPlayed = _gameStateService.State.LastPlayedAt;
        return DateTime.UtcNow - lastPlayed;
    }
}
