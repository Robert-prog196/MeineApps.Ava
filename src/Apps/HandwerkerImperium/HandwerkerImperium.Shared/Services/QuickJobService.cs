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
    private readonly Random _random = new();
    private static readonly TimeSpan RotationInterval = TimeSpan.FromMinutes(15);

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
        foreach (var job in jobs)
        {
            PopulateDisplayFields(job);
        }
        return jobs;
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
            var workshopType = unlockedTypes[_random.Next(unlockedTypes.Count)];
            var miniGameType = AvailableMiniGames[_random.Next(AvailableMiniGames.Length)];
            var titleKey = TitleKeys[_random.Next(TitleKeys.Length)];

            // Belohnung skaliert mit Level
            var reward = Math.Min(100m, 20m + level * 5m);
            var xpReward = Math.Min(25, 5 + level * 2);

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
                var workshopType = unlockedTypes[_random.Next(unlockedTypes.Count)];
                var miniGameType = AvailableMiniGames[_random.Next(AvailableMiniGames.Length)];
                var titleKey = TitleKeys[_random.Next(TitleKeys.Length)];
                var reward = Math.Min(100m, 20m + level * 5m);
                var xpReward = Math.Min(25, 5 + level * 2);

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
    /// Wird vom MainViewModel aufgerufen um das Event zu feuern.
    /// </summary>
    public void NotifyJobCompleted(QuickJob job)
    {
        QuickJobCompleted?.Invoke(this, job);
    }
}
