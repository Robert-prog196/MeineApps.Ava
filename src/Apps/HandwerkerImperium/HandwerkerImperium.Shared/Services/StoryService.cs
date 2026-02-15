using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Verwaltet die Story-Kapitel von Meister Hans.
/// Prüft bei Fortschritt ob neue Kapitel freigeschaltet werden.
/// </summary>
public class StoryService : IStoryService
{
    private readonly IGameStateService _gameStateService;
    private readonly List<StoryChapter> _chapters;

    public StoryService(IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
        _chapters = CreateChapters();
    }

    /// <summary>
    /// Prüft ob ein neues Story-Kapitel freigeschaltet wurde.
    /// Gibt das Kapitel zurück oder null.
    /// </summary>
    public StoryChapter? CheckForNewChapter()
    {
        var state = _gameStateService.State;

        foreach (var chapter in _chapters)
        {
            // Bereits gesehen? Überspringen
            if (state.ViewedStoryIds.Contains(chapter.Id))
                continue;

            // Bedingung erfüllt?
            if (IsChapterUnlocked(chapter, state))
            {
                state.PendingStoryId = chapter.Id;
                _gameStateService.MarkDirty();
                return chapter;
            }

            // Kapitel sind geordnet - wenn dieses nicht freigeschaltet ist,
            // sind spätere auch nicht freigeschaltet
            break;
        }

        return null;
    }

    /// <summary>
    /// Markiert ein Kapitel als gesehen und gibt Belohnungen.
    /// </summary>
    public void MarkChapterViewed(string chapterId)
    {
        var state = _gameStateService.State;
        if (!state.ViewedStoryIds.Contains(chapterId))
        {
            state.ViewedStoryIds.Add(chapterId);
        }

        if (state.PendingStoryId == chapterId)
        {
            state.PendingStoryId = null;
        }

        // Belohnungen vergeben
        var chapter = _chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter != null)
        {
            if (chapter.MoneyReward > 0)
                _gameStateService.AddMoney(chapter.MoneyReward);
            if (chapter.GoldenScrewReward > 0)
                _gameStateService.AddGoldenScrews(chapter.GoldenScrewReward);
            if (chapter.XpReward > 0)
                _gameStateService.AddXp(chapter.XpReward);
        }

        _gameStateService.MarkDirty();
    }

    /// <summary>
    /// Gibt das aktuell wartende (ungesehene) Kapitel zurück.
    /// </summary>
    public StoryChapter? GetPendingChapter()
    {
        var pendingId = _gameStateService.State.PendingStoryId;
        if (string.IsNullOrEmpty(pendingId)) return null;
        return _chapters.FirstOrDefault(c => c.Id == pendingId);
    }

    /// <summary>
    /// Gibt alle Kapitel zurück (für Story-Log).
    /// </summary>
    public IReadOnlyList<StoryChapter> GetAllChapters() => _chapters;

    /// <summary>
    /// Anzahl gesehener Kapitel / Gesamtanzahl.
    /// </summary>
    public (int viewed, int total) GetProgress()
    {
        var viewed = _gameStateService.State.ViewedStoryIds.Count;
        return (viewed, _chapters.Count);
    }

    private bool IsChapterUnlocked(StoryChapter chapter, GameState state)
    {
        // Alle gesetzten Bedingungen müssen erfüllt sein
        if (chapter.RequiredPlayerLevel > 0 && state.PlayerLevel < chapter.RequiredPlayerLevel)
            return false;
        if (chapter.RequiredWorkshopCount > 0 && state.Workshops.Count(w => w.Level > 1) < chapter.RequiredWorkshopCount)
            return false;
        if (chapter.RequiredTotalOrders > 0 && state.TotalOrdersCompleted < chapter.RequiredTotalOrders)
            return false;
        if (chapter.RequiredPrestige > 0 && state.Prestige.TotalPrestigeCount < chapter.RequiredPrestige)
            return false;
        return true;
    }

    private static List<StoryChapter> CreateChapters() =>
    [
        new()
        {
            Id = "ch01_welcome",
            ChapterNumber = 1,
            TitleKey = "Story_Ch01_Title",
            TextKey = "Story_Ch01_Text",
            TitleFallback = "Willkommen, Lehrling!",
            TextFallback = "Ich bin Meister Hans, der erfahrenste Handwerker der Stadt. Du hast Potenzial, das sehe ich! Zusammen bauen wir ein Handwerker-Imperium auf. Fang an, indem du deine Schreinerei aufrüstest!",
            RequiredPlayerLevel = 1,
            MoneyReward = 100,
            XpReward = 10,
            Mood = "happy"
        },
        new()
        {
            Id = "ch02_first_upgrade",
            ChapterNumber = 2,
            TitleKey = "Story_Ch02_Title",
            TextKey = "Story_Ch02_Text",
            TitleFallback = "Gute Arbeit!",
            TextFallback = "Du hast deine erste Werkstatt aufgerüstet! So fängt jeder Meister an. Jetzt brauchst du einen Lehrling, der dir hilft. Ein guter Arbeiter ist Gold wert!",
            RequiredPlayerLevel = 3,
            MoneyReward = 500,
            GoldenScrewReward = 1,
            XpReward = 25,
            Mood = "proud"
        },
        new()
        {
            Id = "ch03_new_workshop",
            ChapterNumber = 3,
            TitleKey = "Story_Ch03_Title",
            TextKey = "Story_Ch03_Text",
            TitleFallback = "Zeit für Expansion!",
            TextFallback = "Die Klempner-Zunft sucht Nachwuchs! Schalte die Klempnerei frei und verdopple dein Einkommen. Mehrere Standbeine sind das Geheimnis eines erfolgreichen Imperiums.",
            RequiredPlayerLevel = 5,
            RequiredWorkshopCount = 1,
            MoneyReward = 1000,
            GoldenScrewReward = 2,
            XpReward = 50,
            Mood = "excited"
        },
        new()
        {
            Id = "ch04_first_order",
            ChapterNumber = 4,
            TitleKey = "Story_Ch04_Title",
            TextKey = "Story_Ch04_Text",
            TitleFallback = "Dein erster Auftrag!",
            TextFallback = "Ein Kunde hat angerufen! Aufträge bringen Extra-Geld und Erfahrung. Zeig was du drauf hast - je besser deine Arbeit, desto höher die Belohnung!",
            RequiredTotalOrders = 1,
            MoneyReward = 2000,
            XpReward = 75,
            Mood = "excited"
        },
        new()
        {
            Id = "ch05_growing_business",
            ChapterNumber = 5,
            TitleKey = "Story_Ch05_Title",
            TextKey = "Story_Ch05_Text",
            TitleFallback = "Wachsendes Geschäft",
            TextFallback = "Drei Werkstätten! Du wirst langsam zum echten Unternehmer. Denk daran: Jede Werkstatt hat eigene Stärken. Investiere klug und halte die Kosten im Blick!",
            RequiredPlayerLevel = 10,
            RequiredWorkshopCount = 3,
            MoneyReward = 5000,
            GoldenScrewReward = 3,
            XpReward = 100,
            Mood = "proud"
        },
        new()
        {
            Id = "ch06_master_tools",
            ChapterNumber = 6,
            TitleKey = "Story_Ch06_Title",
            TextKey = "Story_Ch06_Text",
            TitleFallback = "Meisterwerkzeuge",
            TextFallback = "Ich habe gehört, du sammelst Meisterwerkzeuge! Jedes Werkzeug hat besondere Kräfte. Sammle sie alle und du wirst unaufhaltbar!",
            RequiredPlayerLevel = 15,
            MoneyReward = 10000,
            GoldenScrewReward = 5,
            XpReward = 150,
            Mood = "happy"
        },
        new()
        {
            Id = "ch07_empire_builder",
            ChapterNumber = 7,
            TitleKey = "Story_Ch07_Title",
            TextKey = "Story_Ch07_Text",
            TitleFallback = "Der Imperium-Erbauer",
            TextFallback = "Fünf Werkstätten und ein florierendes Geschäft! Du bist auf dem besten Weg, das größte Handwerker-Imperium der Stadt aufzubauen. Weiter so!",
            RequiredPlayerLevel = 25,
            RequiredWorkshopCount = 5,
            MoneyReward = 50000,
            GoldenScrewReward = 10,
            XpReward = 250,
            Mood = "proud"
        },
        new()
        {
            Id = "ch08_prestige",
            ChapterNumber = 8,
            TitleKey = "Story_Ch08_Title",
            TextKey = "Story_Ch08_Text",
            TitleFallback = "Neuanfang mit Erfahrung",
            TextFallback = "Manchmal muss man einen Schritt zurück machen, um zwei vorwärts zu gehen. Prestige gibt dir permanente Boni für deinen nächsten Durchlauf. Trau dich!",
            RequiredPrestige = 1,
            MoneyReward = 100000,
            GoldenScrewReward = 20,
            XpReward = 500,
            Mood = "concerned"
        },
        new()
        {
            Id = "ch09_legend",
            ChapterNumber = 9,
            TitleKey = "Story_Ch09_Title",
            TextKey = "Story_Ch09_Text",
            TitleFallback = "Die Legende",
            TextFallback = "Du hast es geschafft! Alle Werkstätten, zahllose Aufträge, und dein Name ist in der ganzen Stadt bekannt. Ich bin stolz auf dich, Meister!",
            RequiredPlayerLevel = 50,
            RequiredWorkshopCount = 7,
            RequiredTotalOrders = 50,
            MoneyReward = 500000,
            GoldenScrewReward = 50,
            XpReward = 1000,
            Mood = "proud"
        },
        new()
        {
            Id = "ch10_grandmaster",
            ChapterNumber = 10,
            TitleKey = "Story_Ch10_Title",
            TextKey = "Story_Ch10_Text",
            TitleFallback = "Großmeister",
            TextFallback = "Level 100! Du bist jetzt der Großmeister der Handwerker-Gilde. Dein Imperium ist ein Vorbild für alle. Aber vergiss nicht: Ein wahrer Meister hört nie auf zu lernen!",
            RequiredPlayerLevel = 100,
            MoneyReward = 1000000,
            GoldenScrewReward = 100,
            XpReward = 2000,
            Mood = "excited"
        }
    ];
}
