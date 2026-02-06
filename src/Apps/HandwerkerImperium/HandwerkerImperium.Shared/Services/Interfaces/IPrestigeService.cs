namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Service for managing the prestige system.
/// Players can prestige at level 30+ to reset progress but gain permanent multipliers.
/// </summary>
public interface IPrestigeService
{
    /// <summary>
    /// Current prestige level.
    /// </summary>
    int CurrentPrestigeLevel { get; }

    /// <summary>
    /// Current prestige multiplier (1.0 = no bonus).
    /// </summary>
    decimal CurrentMultiplier { get; }

    /// <summary>
    /// Whether the player can currently prestige (level 30+).
    /// </summary>
    bool CanPrestige { get; }

    /// <summary>
    /// The multiplier the player would get if they prestige now.
    /// </summary>
    decimal PotentialMultiplier { get; }

    /// <summary>
    /// The bonus percentage increase if prestige now.
    /// </summary>
    decimal PotentialBonusPercent { get; }

    /// <summary>
    /// Minimum level required to prestige.
    /// </summary>
    int MinimumLevel { get; }

    /// <summary>
    /// Event fired when prestige is completed.
    /// </summary>
    event EventHandler? PrestigeCompleted;

    /// <summary>
    /// Calculates the potential multiplier for the current state.
    /// </summary>
    decimal CalculatePotentialMultiplier();

    /// <summary>
    /// Performs the prestige reset and applies the multiplier.
    /// Returns true if successful.
    /// </summary>
    Task<bool> PerformPrestigeAsync();
}
