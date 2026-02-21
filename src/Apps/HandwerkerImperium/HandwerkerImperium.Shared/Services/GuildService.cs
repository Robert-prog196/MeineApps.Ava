using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Verwaltet Innungen/Gilden mit echten Spielern via Play Games Leaderboards.
/// Jede der 5 Gilden hat ein eigenes Leaderboard. Spieler submittiert wöchentlichen
/// Beitrag als Score, Leaderboard-Einträge sind die echten Gildenmitglieder.
/// </summary>
public class GuildService : IGuildService
{
    private readonly IGameStateService _gameState;
    private readonly IPlayGamesService _playGamesService;

    public event Action? GuildUpdated;

    public GuildService(IGameStateService gameState, IPlayGamesService playGamesService)
    {
        _gameState = gameState;
        _playGamesService = playGamesService;
    }

    public void JoinGuild(string guildId)
    {
        var state = _gameState.State;

        // Bereits in einer Gilde?
        if (state.Guild != null) return;

        // Gilde erstellen (ohne simulierte Mitglieder)
        state.Guild = Guild.Create(guildId, state.PlayerLevel);

        _gameState.MarkDirty();
        GuildUpdated?.Invoke();
    }

    public void LeaveGuild()
    {
        var state = _gameState.State;
        if (state.Guild == null) return;

        state.Guild = null;

        _gameState.MarkDirty();
        GuildUpdated?.Invoke();
    }

    public async Task ContributeToGoalAsync(decimal amount)
    {
        var guild = _gameState.State.Guild;
        if (guild == null || amount <= 0) return;

        // Spieler muss genug Geld haben
        if (!_gameState.TrySpendMoney(amount)) return;

        guild.WeeklyProgress += amount;
        guild.PlayerContribution += amount;

        // Score an Play Games Leaderboard submittieren
        if (_playGamesService.IsSignedIn && !string.IsNullOrEmpty(guild.LeaderboardId))
        {
            await _playGamesService.SubmitScoreAsync(guild.LeaderboardId, (long)guild.PlayerContribution);
        }

        _gameState.MarkDirty();
        GuildUpdated?.Invoke();
    }

    public async Task<List<PlayGamesLeaderboardEntry>> RefreshGuildMembersAsync()
    {
        var guild = _gameState.State.Guild;
        if (guild == null) return [];

        // Leaderboard-Einträge laden
        if (_playGamesService.IsSignedIn && !string.IsNullOrEmpty(guild.LeaderboardId))
        {
            var entries = await _playGamesService.LoadLeaderboardScoresAsync(guild.LeaderboardId, 25);

            // Spieler-Rang aus den Einträgen aktualisieren
            var playerEntry = entries.FirstOrDefault(e => e.PlayerName == _playGamesService.PlayerDisplayName);
            if (playerEntry != null)
            {
                guild.PlayerRank = playerEntry.Rank;
            }

            guild.TotalMembers = Math.Max(1, entries.Count);
            _gameState.MarkDirty();

            return entries;
        }

        return [];
    }

    public void CheckWeeklyGoalCompletion()
    {
        var guild = _gameState.State.Guild;
        if (guild == null) return;

        var currentMonday = GetCurrentMonday();

        // Wochenwechsel prüfen → Ziel zurücksetzen
        if (guild.LastWeeklyReset < currentMonday)
        {
            // Vor dem Reset: Prüfen ob letztes Wochenziel erreicht wurde
            if (guild.IsWeeklyGoalReached)
            {
                // Belohnung: Goldschrauben basierend auf Gilden-Level
                int screwReward = Math.Min(50, 5 + guild.Level * 2);
                _gameState.AddGoldenScrews(screwReward);

                guild.TotalWeeksCompleted++;
                guild.Level++;
            }

            // Wochenziel zurücksetzen
            guild.WeeklyProgress = 0;
            guild.PlayerContribution = 0;
            guild.PlayerRank = 0;

            // Neues Wochenziel skaliert mit Level
            guild.WeeklyGoal = Math.Max(100_000m, _gameState.State.PlayerLevel * 10_000m) *
                               (1m + guild.Level * 0.1m);
            guild.LastWeeklyReset = currentMonday;

            _gameState.MarkDirty();
            GuildUpdated?.Invoke();
        }
    }

    public List<GuildDefinition> GetAvailableGuilds()
    {
        return Guild.GetAvailableGuilds();
    }

    private static DateTime GetCurrentMonday()
    {
        var today = DateTime.UtcNow.Date;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }
}
