using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Verwaltet taegliche Herausforderungen mit Belohnungen.
/// </summary>
public interface IDailyChallengeService
{
    DailyChallengeState GetState();
    void CheckAndResetIfNewDay();
    bool ClaimReward(string challengeId);
    bool ClaimAllCompletedBonus();
    bool AreAllCompleted { get; }
    bool HasUnclaimedRewards { get; }
    decimal AllCompletedBonusAmount { get; }
}
