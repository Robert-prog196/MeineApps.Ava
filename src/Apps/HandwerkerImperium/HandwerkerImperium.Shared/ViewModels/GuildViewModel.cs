using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerImperium.ViewModels;

/// <summary>
/// ViewModel für das Innungs-/Gilden-System mit echten Spielern via Play Games Leaderboards.
/// Zeigt Gilden-Info, Leaderboard-Mitglieder, Wochenziel und verfügbare Gilden.
/// </summary>
public partial class GuildViewModel : ObservableObject
{
    private readonly IGameStateService _gameStateService;
    private readonly IGuildService _guildService;
    private readonly IPlayGamesService _playGamesService;
    private readonly ILocalizationService _localizationService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public event Action<string>? NavigationRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private ObservableCollection<GuildMemberDisplay> _members = [];

    /// <summary>
    /// Name der aktuellen Gilde (wenn beigetreten).
    /// </summary>
    [ObservableProperty]
    private string _currentGuildName = "";

    /// <summary>
    /// Level-Anzeige der aktuellen Gilde.
    /// </summary>
    [ObservableProperty]
    private string _currentGuildLevelDisplay = "";

    [ObservableProperty]
    private int _guildLevel;

    /// <summary>
    /// Fortschritts-Anzeige zum Wochenziel (z.B. "50.000 / 100.000").
    /// </summary>
    [ObservableProperty]
    private string _goalProgressDisplay = "";

    [ObservableProperty]
    private double _goalProgress;

    [ObservableProperty]
    private decimal _incomeBonus;

    /// <summary>
    /// Einkommens-Bonus Anzeige im Header-Badge (z.B. "+5%").
    /// </summary>
    [ObservableProperty]
    private string _incomeBonusDisplay = "";

    /// <summary>
    /// Detail-Anzeige des Einkommens-Bonus (z.B. "+5% Einkommen durch Innung").
    /// </summary>
    [ObservableProperty]
    private string _incomeBonusDetailDisplay = "";

    /// <summary>
    /// Header-Anzeige der Mitglieder-Sektion (z.B. "Mitglieder (5)").
    /// </summary>
    [ObservableProperty]
    private string _membersHeaderDisplay = "";

    [ObservableProperty]
    private bool _isInGuild;

    [ObservableProperty]
    private ObservableCollection<GuildDisplayItem> _availableGuilds = [];

    /// <summary>
    /// Ob Play Games angemeldet ist (für Login-Hinweis).
    /// </summary>
    [ObservableProperty]
    private bool _isPlayGamesSignedIn;

    /// <summary>
    /// Ob Leaderboard-Daten gerade geladen werden.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingMembers;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public GuildViewModel(
        IGameStateService gameStateService,
        IGuildService guildService,
        IPlayGamesService playGamesService,
        ILocalizationService localizationService)
    {
        _gameStateService = gameStateService;
        _guildService = guildService;
        _playGamesService = playGamesService;
        _localizationService = localizationService;

        UpdateLocalizedTexts();
        RefreshGuild();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task JoinGuildAsync(GuildDisplayItem? item)
    {
        if (item == null) return;

        _guildService.JoinGuild(item.Id);
        RefreshGuild();

        // Leaderboard-Mitglieder laden
        await LoadMembersAsync();
    }

    [RelayCommand]
    private void LeaveGuild()
    {
        _guildService.LeaveGuild();
        RefreshGuild();
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aktualisiert alle Gilden-Daten aus dem State (synchron, ohne Leaderboard-Load).
    /// </summary>
    public void RefreshGuild()
    {
        var state = _gameStateService.State;
        var guild = state.Guild;

        IsInGuild = guild != null;
        IsPlayGamesSignedIn = _playGamesService.IsSignedIn;

        if (guild != null)
        {
            CurrentGuildName = _localizationService.GetString(guild.NameKey) ?? guild.NameKey;
            GuildLevel = guild.Level;
            CurrentGuildLevelDisplay = $"Lv.{guild.Level}";
            GoalProgress = guild.WeeklyGoalProgress;
            GoalProgressDisplay = $"{MoneyFormatter.Format(guild.WeeklyProgress, 0)} / {MoneyFormatter.Format(guild.WeeklyGoal, 0)}";
            IncomeBonus = guild.IncomeBonus;
            IncomeBonusDisplay = $"+{guild.IncomeBonus * 100:F0}%";
            IncomeBonusDetailDisplay = $"+{guild.IncomeBonus * 100:F0}% {(_localizationService.GetString("GuildIncomeBonus") ?? "Einkommens-Bonus")}";

            // Spieler als einziges Mitglied (bis Leaderboard geladen)
            if (Members.Count == 0)
            {
                Members =
                [
                    new GuildMemberDisplay
                    {
                        Name = _playGamesService.PlayerDisplayName
                               ?? (_localizationService.GetString("You") ?? "Du"),
                        RoleDisplay = guild.PlayerRank > 0 ? $"#{guild.PlayerRank}" : "",
                        ContributionDisplay = MoneyFormatter.Format(guild.PlayerContribution, 0),
                        IsPlayer = true
                    }
                ];
            }

            MembersHeaderDisplay = $"{(_localizationService.GetString("Members") ?? "Mitglieder")} ({Members.Count})";
            AvailableGuilds.Clear();
        }
        else
        {
            CurrentGuildName = "";
            CurrentGuildLevelDisplay = "";
            GuildLevel = 0;
            GoalProgress = 0;
            GoalProgressDisplay = "";
            IncomeBonus = 0;
            IncomeBonusDisplay = "";
            IncomeBonusDetailDisplay = "";
            MembersHeaderDisplay = "";
            Members.Clear();

            // Verfügbare Gilden anzeigen
            BuildAvailableGuilds();
        }
    }

    /// <summary>
    /// Lädt Mitglieder-Daten asynchron aus dem Play Games Leaderboard.
    /// </summary>
    public async Task LoadMembersAsync()
    {
        if (!IsInGuild) return;

        IsLoadingMembers = true;
        try
        {
            var entries = await _guildService.RefreshGuildMembersAsync();
            var memberDisplays = new ObservableCollection<GuildMemberDisplay>();

            var guild = _gameStateService.State.Guild;
            var playerName = _playGamesService.PlayerDisplayName;

            // Spieler immer zuerst
            memberDisplays.Add(new GuildMemberDisplay
            {
                Name = playerName ?? (_localizationService.GetString("You") ?? "Du"),
                RoleDisplay = guild?.PlayerRank > 0 ? $"#{guild.PlayerRank}" : "",
                ContributionDisplay = MoneyFormatter.Format(guild?.PlayerContribution ?? 0, 0),
                IsPlayer = true
            });

            // Leaderboard-Einträge (ohne Spieler selbst)
            foreach (var entry in entries)
            {
                if (entry.PlayerName == playerName) continue;

                memberDisplays.Add(new GuildMemberDisplay
                {
                    Name = entry.PlayerName,
                    RoleDisplay = $"#{entry.Rank}",
                    ContributionDisplay = MoneyFormatter.Format(entry.Score, 0)
                });
            }

            Members = memberDisplays;
            MembersHeaderDisplay = $"{(_localizationService.GetString("Members") ?? "Mitglieder")} ({Members.Count})";
        }
        finally
        {
            IsLoadingMembers = false;
        }
    }

    /// <summary>
    /// Lokalisierte Texte aktualisieren.
    /// </summary>
    public void UpdateLocalizedTexts()
    {
        Title = _localizationService.GetString("Guild") ?? "Innung";
        RefreshGuild();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildAvailableGuilds()
    {
        var guilds = Guild.GetAvailableGuilds();
        var items = new ObservableCollection<GuildDisplayItem>();

        foreach (var guildDef in guilds)
        {
            items.Add(new GuildDisplayItem
            {
                Id = guildDef.Id,
                GuildName = _localizationService.GetString(guildDef.NameKey) ?? guildDef.NameKey,
                GuildColor = guildDef.Color,
                LevelDisplay = "Lv.1",
                MemberCountDisplay = _localizationService.GetString("PlayGamesMembersInfo") ?? "Play Games Spieler",
                BonusDescription = $"+1% {(_localizationService.GetString("Income") ?? "Einkommen")}"
            });
        }

        AvailableGuilds = items;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DISPLAY MODELS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Anzeige-Modell für ein Gildenmitglied im UI.
/// Kommt entweder vom Play Games Leaderboard oder ist der Spieler selbst.
/// </summary>
public class GuildMemberDisplay
{
    public string Name { get; set; } = "";
    public string RoleDisplay { get; set; } = "";
    public string ContributionDisplay { get; set; } = "";
    public bool IsPlayer { get; set; }
}

/// <summary>
/// Anzeige-Modell für eine wählbare Gilde im UI (Gilden-Auswahl).
/// </summary>
public class GuildDisplayItem
{
    public string Id { get; set; } = "";
    public string GuildName { get; set; } = "";
    public string GuildColor { get; set; } = "#D97706";
    public string LevelDisplay { get; set; } = "";
    public string MemberCountDisplay { get; set; } = "";
    public string BonusDescription { get; set; } = "";
}
