using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.Services.Interfaces;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerImperium.Services;

/// <summary>
/// Verwaltet Schnell-Auftraege die alle 15 Minuten rotieren.
/// </summary>
public class QuickJobService : IQuickJobService
{
    private readonly IGameStateService _gameStateService;
    private readonly ILocalizationService _localizationService;
    private static readonly TimeSpan RotationInterval = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximale Anzahl Quick Jobs pro Tag (verhindert Reward-Farming).
    /// </summary>
    public const int MaxQuickJobsPerDay = 20;

    // Verfuegbare MiniGame-Typen fuer Quick Jobs (nur die 4 implementierten)
    private static readonly MiniGameType[] AvailableMiniGames =
    [
        MiniGameType.Sawing,
        MiniGameType.PipePuzzle,
        MiniGameType.WiringGame,
        MiniGameType.PaintingGame
    ];

    private static readonly string[] TitleKeys =
    [
        "QuickRepair", "QuickFix", "ExpressService", "SmallOrder",
        "QuickMeasure", "QuickInstall", "QuickPaint", "QuickCheck"
    ];

    /// <summary>
    /// Belohnungs-Multiplikatoren pro Auftragstyp.
    /// Express-Aufträge sind deutlich lukrativer (Aufschlag für Schnelligkeit).
    /// </summary>
    private static readonly Dictionary<string, decimal> TitleRewardMultipliers = new()
    {
        ["QuickRepair"]     = 0.90m,
        ["QuickFix"]        = 0.85m,
        ["ExpressService"]  = 1.40m,  // Express = teurer
        ["SmallOrder"]      = 0.80m,
        ["QuickMeasure"]    = 0.75m,
        ["QuickInstall"]    = 1.10m,
        ["QuickPaint"]      = 0.95m,
        ["QuickCheck"]      = 1.30m,  // "Express-Prüfung" = teurer
    };

    public event EventHandler<QuickJob>? QuickJobCompleted;

    public QuickJobService(IGameStateService gameStateService, ILocalizationService localizationService)
    {
        _gameStateService = gameStateService;
        _localizationService = localizationService;
    }

    public TimeSpan TimeUntilNextRotation
    {
        get
        {
            var lastRotation = _gameStateService.State.LastQuickJobRotation;
            var nextRotation = lastRotation + RotationInterval;
            var remaining = nextRotation - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public List<QuickJob> GetAvailableJobs()
    {
        var jobs = _gameStateService.State.QuickJobs;
        var level = _gameStateService.State.PlayerLevel;
        foreach (var job in jobs)
        {
            // Belohnungen bei jedem Abruf neu berechnen (skaliert mit aktuellem Einkommen)
            if (!job.IsCompleted)
                RecalculateRewards(job, level);
            PopulateDisplayFields(job);
        }
        return jobs;
    }

    /// <summary>
    /// Berechnet Belohnungen eines QuickJobs neu basierend auf aktuellem Einkommen und Auftragstyp.
    /// </summary>
    private void RecalculateRewards(QuickJob job, int level)
    {
        var (reward, xpReward) = CalculateQuickJobRewards(level, job.TitleKey);
        job.Reward = reward;
        job.XpReward = xpReward;
    }

    public void GenerateJobs(int count = 5)
    {
        var state = _gameStateService.State;
        var level = state.PlayerLevel;

        // Freigeschaltete Workshop-Typen ermitteln
        var unlockedTypes = state.UnlockedWorkshopTypes;
        if (unlockedTypes.Count == 0)
            unlockedTypes = [WorkshopType.Carpenter];

        state.QuickJobs.Clear();
        for (int i = 0; i < count; i++)
        {
            var workshopType = unlockedTypes[Random.Shared.Next(unlockedTypes.Count)];
            var miniGameType = AvailableMiniGames[Random.Shared.Next(AvailableMiniGames.Length)];
            var titleKey = TitleKeys[Random.Shared.Next(TitleKeys.Length)];

            // Belohnung skaliert mit Level, Einkommen und Auftragstyp
            var (reward, xpReward) = CalculateQuickJobRewards(level, titleKey);

            state.QuickJobs.Add(new QuickJob
            {
                WorkshopType = workshopType,
                Difficulty = OrderDifficulty.Easy,
                MiniGameType = miniGameType,
                Reward = reward,
                XpReward = xpReward,
                TitleKey = titleKey
            });
        }

        state.LastQuickJobRotation = DateTime.UtcNow;
        _gameStateService.MarkDirty();
    }

    public bool NeedsRotation()
    {
        return DateTime.UtcNow - _gameStateService.State.LastQuickJobRotation > RotationInterval;
    }

    public void RotateIfNeeded()
    {
        // Tages-Counter zurücksetzen wenn neuer Tag
        ResetDailyCounterIfNewDay();

        if (!NeedsRotation()) return;

        var state = _gameStateService.State;

        // Erledigte Jobs entfernen
        state.QuickJobs.RemoveAll(j => j.IsCompleted);

        // Neue Jobs generieren bis 5 erreicht
        var missing = 5 - state.QuickJobs.Count;
        if (missing > 0)
        {
            var unlockedTypes = state.UnlockedWorkshopTypes;
            if (unlockedTypes.Count == 0) unlockedTypes = [WorkshopType.Carpenter];
            var level = state.PlayerLevel;

            for (int i = 0; i < missing; i++)
            {
                var workshopType = unlockedTypes[Random.Shared.Next(unlockedTypes.Count)];
                var miniGameType = AvailableMiniGames[Random.Shared.Next(AvailableMiniGames.Length)];
                var titleKey = TitleKeys[Random.Shared.Next(TitleKeys.Length)];
                var (reward, xpReward) = CalculateQuickJobRewards(level, titleKey);

                state.QuickJobs.Add(new QuickJob
                {
                    WorkshopType = workshopType,
                    Difficulty = OrderDifficulty.Easy,
                    MiniGameType = miniGameType,
                    Reward = reward,
                    XpReward = xpReward,
                    TitleKey = titleKey
                });
            }
        }

        state.LastQuickJobRotation = DateTime.UtcNow;
        _gameStateService.MarkDirty();
    }

    /// <summary>
    /// Berechnet QuickJob-Belohnungen basierend auf Level, aktuellem Netto-Einkommen und Auftragstyp.
    /// Express-Aufträge haben höhere Belohnungen (Aufschlag), kleine Aufträge weniger.
    /// </summary>
    private (decimal reward, int xpReward) CalculateQuickJobRewards(int level, string titleKey = "")
    {
        // Basis: ~5 Min Netto-Einkommen (Mindestens Level * 50)
        var fiveMinIncome = Math.Max(0m, _gameStateService.State.NetIncomePerSecond) * 300m;
        var baseReward = Math.Max(20m + level * 50m, fiveMinIncome);

        // Typ-Multiplikator anwenden (Express = teurer, kleine Aufträge = günstiger)
        var multiplier = 1.0m;
        if (!string.IsNullOrEmpty(titleKey) && TitleRewardMultipliers.TryGetValue(titleKey, out var m))
            multiplier = m;
        var reward = baseReward * multiplier;

        // XP skaliert mit Level + Bonus für Express-Aufträge
        var xpReward = 5 + level * 3;
        if (multiplier > 1.0m)
            xpReward = (int)(xpReward * multiplier);

        return (Math.Round(reward, 0), xpReward);
    }

    /// <summary>
    /// Fuellt die Display-Properties eines QuickJobs mit lokalisierten Texten.
    /// </summary>
    private void PopulateDisplayFields(QuickJob job)
    {
        var title = _localizationService.GetString(job.TitleKey);
        job.DisplayTitle = string.IsNullOrEmpty(title) ? job.TitleKey : title;
        job.DisplayWorkshopName = _localizationService.GetString(job.WorkshopType.GetLocalizationKey());
        job.RewardDisplay = $"{job.Reward:N0} € + {job.XpReward} XP";
    }

    /// <summary>
    /// Prüft ob das tägliche Quick-Job-Limit erreicht ist.
    /// </summary>
    public bool IsDailyLimitReached
    {
        get
        {
            ResetDailyCounterIfNewDay();
            return _gameStateService.State.QuickJobsCompletedToday >= MaxQuickJobsPerDay;
        }
    }

    /// <summary>
    /// Verbleibende Quick Jobs heute.
    /// </summary>
    public int RemainingJobsToday
    {
        get
        {
            ResetDailyCounterIfNewDay();
            return Math.Max(0, MaxQuickJobsPerDay - _gameStateService.State.QuickJobsCompletedToday);
        }
    }

    /// <summary>
    /// Wird vom MainViewModel aufgerufen wenn ein QuickJob abgeschlossen wird.
    /// Erhöht Tages-Counter und feuert Event.
    /// </summary>
    public void NotifyJobCompleted(QuickJob job)
    {
        ResetDailyCounterIfNewDay();
        _gameStateService.State.QuickJobsCompletedToday++;
        QuickJobCompleted?.Invoke(this, job);
    }

    /// <summary>
    /// Setzt den Tages-Counter zurück wenn ein neuer Tag (UTC) begonnen hat.
    /// </summary>
    private void ResetDailyCounterIfNewDay()
    {
        var state = _gameStateService.State;
        if (DateTime.UtcNow.Date > state.LastQuickJobDailyReset.Date)
        {
            state.QuickJobsCompletedToday = 0;
            state.LastQuickJobDailyReset = DateTime.UtcNow;
        }
    }
}
