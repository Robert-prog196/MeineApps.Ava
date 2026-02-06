using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Manages the prestige system.
/// Players can reset progress at level 30+ to gain permanent income multipliers.
/// </summary>
public class PrestigeService : IPrestigeService
{
    private readonly IGameStateService _gameStateService;
    private readonly ISaveGameService _saveGameService;

    public const int MINIMUM_PRESTIGE_LEVEL = 20;
    public const decimal MONEY_DIVISOR = 1_000_000m; // 1 million

    public event EventHandler? PrestigeCompleted;

    public PrestigeService(IGameStateService gameStateService, ISaveGameService saveGameService)
    {
        _gameStateService = gameStateService;
        _saveGameService = saveGameService;
    }

    public int CurrentPrestigeLevel => _gameStateService.State.PrestigeLevel;

    public decimal CurrentMultiplier => _gameStateService.State.PrestigeMultiplier;

    public bool CanPrestige => _gameStateService.State.PlayerLevel >= MINIMUM_PRESTIGE_LEVEL;

    public int MinimumLevel => MINIMUM_PRESTIGE_LEVEL;

    public decimal PotentialMultiplier => CalculatePotentialMultiplier();

    public decimal PotentialBonusPercent
    {
        get
        {
            var potentialMultiplier = CalculatePotentialMultiplier();
            // Convert multiplier to percentage increase
            // e.g., 1.316 -> 31.6%
            return (potentialMultiplier - 1m) * 100m;
        }
    }

    /// <summary>
    /// Calculates the potential multiplier based on total money earned.
    /// Formula: 1 + sqrt(TotalMoneyEarned / 1,000,000) * 0.10
    /// Example: 10M earned -> 1 + sqrt(10) * 0.10 = 1.316 (31.6% bonus)
    /// </summary>
    public decimal CalculatePotentialMultiplier()
    {
        var totalMoney = _gameStateService.State.TotalMoneyEarned;
        if (totalMoney <= 0) return 1m;

        // sqrt(TotalMoneyEarned / 1,000,000) * 10% bonus
        var ratio = (double)(totalMoney / MONEY_DIVISOR);
        var sqrtRatio = Math.Sqrt(ratio);
        var bonus = (decimal)sqrtRatio * 0.10m;

        // New multiplier = current multiplier + bonus (stacks with previous prestiges)
        var newMultiplier = CurrentMultiplier + bonus;

        // Round to 3 decimal places
        return Math.Round(newMultiplier, 3);
    }

    public async Task<bool> PerformPrestigeAsync()
    {
        if (!CanPrestige) return false;

        var state = _gameStateService.State;

        // Calculate new multiplier
        var newMultiplier = CalculatePotentialMultiplier();

        // Increment prestige level
        state.PrestigeLevel++;

        // Apply new multiplier
        state.PrestigeMultiplier = newMultiplier;

        // Reset progress (keep: PrestigeLevel, PrestigeMultiplier, Achievements, Premium, Settings)
        ResetProgress(state);

        // Save game
        await _saveGameService.SaveAsync();

        // Fire event
        PrestigeCompleted?.Invoke(this, EventArgs.Empty);

        return true;
    }

    private static void ResetProgress(GameState state)
    {
        // Reset player progress
        state.PlayerLevel = 1;
        state.CurrentXp = 0;
        state.TotalXp = 0;

        // Reset money (keep TotalMoneyEarned for next prestige calculation!)
        state.Money = 100m; // Starting money
        // Note: TotalMoneyEarned is NOT reset - it's used for prestige bonus calculation
        state.TotalMoneySpent = 0m;

        // Reset workshops to level 1
        foreach (var workshop in state.Workshops)
        {
            workshop.Level = workshop.Type == WorkshopType.Carpenter ? 1 : 0;
            workshop.Workers.Clear();
            workshop.TotalEarned = 0m;
            workshop.OrdersCompleted = 0;
        }

        // Clear orders
        state.AvailableOrders.Clear();
        state.ActiveOrder = null;

        // Reset statistics
        state.TotalOrdersCompleted = 0;
        state.TotalMiniGamesPlayed = 0;
        state.PerfectRatings = 0;
        state.PerfectStreak = 0;
        state.BestPerfectStreak = 0;

        // Reset boosts
        state.SpeedBoostEndTime = DateTime.MinValue;
        state.XpBoostEndTime = DateTime.MinValue;

        // Reset daily rewards (optional - could keep streak)
        state.DailyRewardStreak = 0;
        state.LastDailyRewardClaim = DateTime.MinValue;

        // Keep: UnlockedAchievements, IsPremium, TutorialCompleted, Settings
    }
}
