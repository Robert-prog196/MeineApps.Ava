namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Verwaltet wöchentliche Missionen mit höheren Belohnungen als Daily Challenges.
/// </summary>
public interface IWeeklyMissionService
{
    /// <summary>
    /// Wird ausgelöst wenn sich der Fortschritt einer Mission ändert.
    /// </summary>
    event Action? MissionProgressChanged;

    /// <summary>
    /// Initialisiert Event-Subscriptions auf GameStateService.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Prüft ob Montag 00:00 UTC seit letztem Reset vergangen ist und generiert ggf. neue Missionen.
    /// </summary>
    void CheckAndResetIfNewWeek();

    /// <summary>
    /// Beansprucht die Belohnung einer abgeschlossenen Mission.
    /// </summary>
    void ClaimMission(string missionId);

    /// <summary>
    /// Beansprucht den Bonus wenn alle 5 Missionen abgeschlossen sind (50 Goldschrauben).
    /// </summary>
    void ClaimAllCompletedBonus();
}
