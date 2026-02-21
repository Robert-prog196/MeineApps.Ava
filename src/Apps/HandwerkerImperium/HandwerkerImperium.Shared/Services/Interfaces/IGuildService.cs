using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Service für Innungen/Gilden mit echten Spielern via Play Games Leaderboards.
/// </summary>
public interface IGuildService
{
    /// <summary>Feuert wenn sich der Gilden-Zustand ändert.</summary>
    event Action? GuildUpdated;

    /// <summary>Tritt einer Gilde bei.</summary>
    void JoinGuild(string guildId);

    /// <summary>Verlässt die aktuelle Gilde.</summary>
    void LeaveGuild();

    /// <summary>Trägt Geld zum wöchentlichen Gildenziel bei und submittiert Score an Leaderboard.</summary>
    Task ContributeToGoalAsync(decimal amount);

    /// <summary>Lädt aktuelle Mitglieder-Daten aus dem Play Games Leaderboard.</summary>
    Task<List<PlayGamesLeaderboardEntry>> RefreshGuildMembersAsync();

    /// <summary>Prüft ob das Wochenziel erreicht wurde und verteilt Belohnungen.</summary>
    void CheckWeeklyGoalCompletion();

    /// <summary>Gibt die verfügbaren Gilden-Definitionen zurück.</summary>
    List<GuildDefinition> GetAvailableGuilds();
}
