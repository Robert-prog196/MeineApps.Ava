using BomberBlast.Models;
using BomberBlast.Models.Entities;

namespace BomberBlast.Services;

/// <summary>
/// Service für Achievements/Badges
/// </summary>
public interface IAchievementService
{
    /// <summary>Event wenn ein Achievement freigeschaltet wird</summary>
    event EventHandler<Achievement>? AchievementUnlocked;

    /// <summary>Alle Achievements</summary>
    IReadOnlyList<Achievement> Achievements { get; }

    /// <summary>Anzahl freigeschalteter Achievements</summary>
    int UnlockedCount { get; }

    /// <summary>Gesamtzahl Achievements</summary>
    int TotalCount { get; }

    /// <summary>Kumulative Gegner-Kills (für Achievement-Tracking)</summary>
    int TotalEnemyKills { get; }

    /// <summary>Level abgeschlossen - prüft Fortschritts-Achievements</summary>
    Achievement? OnLevelCompleted(int level, int score, int stars, int bombsUsed, float timeRemaining, float timeUsed, bool noDamage);

    /// <summary>Gegner getötet - prüft Kampf-Achievements</summary>
    Achievement? OnEnemyKilled(int totalKills);

    /// <summary>Arcade Wave erreicht - prüft Arcade-Achievements</summary>
    Achievement? OnArcadeWaveReached(int wave);

    /// <summary>Stern-Fortschritt aktualisieren</summary>
    Achievement? OnStarsUpdated(int totalStars);

    /// <summary>Combo erreicht - prüft Combo-Achievements</summary>
    Achievement? OnComboReached(int comboCount);

    /// <summary>Bombe gekickt - prüft Kick-Achievements</summary>
    Achievement? OnBombKicked();

    /// <summary>Power-Bomb platziert - prüft Power-Bomb-Achievements</summary>
    Achievement? OnPowerBombUsed();

    /// <summary>Curse überlebt - prüft Curse-Achievements</summary>
    Achievement? OnCurseSurvived(CurseType curseType);

    /// <summary>Daily Challenge abgeschlossen - prüft Daily-Challenge-Achievements</summary>
    Achievement? OnDailyChallengeCompleted(int totalCompleted, int currentStreak);

    /// <summary>Erzwingt Speichern aller gepufferten Änderungen (Debounce-Flush)</summary>
    void FlushIfDirty();
}
