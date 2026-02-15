namespace BomberBlast.Services;

/// <summary>
/// Service für tägliche Herausforderungen mit einzigartigem Level pro Tag.
/// Deterministisch basierend auf UTC-Datum (gleicher Seed = gleiches Level für alle Spieler).
/// </summary>
public interface IDailyChallengeService
{
    /// <summary>Ob die heutige Challenge bereits gespielt wurde</summary>
    bool IsCompletedToday { get; }

    /// <summary>Bester Score für die heutige Challenge (0 wenn noch nicht gespielt)</summary>
    int TodayBestScore { get; }

    /// <summary>Anzahl der abgeschlossenen Daily Challenges insgesamt</summary>
    int TotalCompleted { get; }

    /// <summary>Aktuelle Streak (Tage in Folge gespielt)</summary>
    int CurrentStreak { get; }

    /// <summary>Längste Streak aller Zeiten</summary>
    int LongestStreak { get; }

    /// <summary>Challenge-Score melden. Gibt true zurück wenn neuer Bestwert.</summary>
    bool SubmitScore(int score);

    /// <summary>Seed für das heutige Level (deterministisch aus Datum)</summary>
    int GetTodaySeed();

    /// <summary>Coin-Belohnung für den aktuellen Streak-Bonus berechnen</summary>
    int GetStreakBonus();
}
