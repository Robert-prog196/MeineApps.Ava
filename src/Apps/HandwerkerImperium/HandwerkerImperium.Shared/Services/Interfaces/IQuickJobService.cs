using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Verwaltet Schnell-Auftraege (Quick Jobs) die alle 15 Minuten rotieren.
/// </summary>
public interface IQuickJobService
{
    List<QuickJob> GetAvailableJobs();
    void GenerateJobs(int count = 5);
    bool NeedsRotation();
    void RotateIfNeeded();
    TimeSpan TimeUntilNextRotation { get; }
    /// <summary>Maximale Quick Jobs pro Tag (skaliert mit Prestige).</summary>
    int MaxDailyJobs { get; }
    event EventHandler<QuickJob>? QuickJobCompleted;
}
