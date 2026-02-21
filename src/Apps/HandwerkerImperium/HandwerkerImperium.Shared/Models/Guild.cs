using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Eine Innung/Gilde, basierend auf Google Play Games Leaderboards.
/// Mitglieder kommen aus echten Leaderboard-Einträgen, nicht simuliert.
/// </summary>
public class Guild
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("nameKey")]
    public string NameKey { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("weeklyGoal")]
    public decimal WeeklyGoal { get; set; }

    [JsonPropertyName("weeklyProgress")]
    public decimal WeeklyProgress { get; set; }

    [JsonPropertyName("lastWeeklyReset")]
    public DateTime LastWeeklyReset { get; set; } = DateTime.MinValue;

    [JsonPropertyName("totalWeeksCompleted")]
    public int TotalWeeksCompleted { get; set; }

    [JsonPropertyName("playerContribution")]
    public decimal PlayerContribution { get; set; }

    /// <summary>
    /// Leaderboard-ID für diese Gilde (aus Play Console).
    /// </summary>
    [JsonPropertyName("leaderboardId")]
    public string LeaderboardId { get; set; } = "";

    /// <summary>
    /// Spieler-Rang im Leaderboard (0 = nicht platziert).
    /// </summary>
    [JsonPropertyName("playerRank")]
    public int PlayerRank { get; set; }

    /// <summary>
    /// Gesamtzahl der Mitglieder (aus Leaderboard).
    /// </summary>
    [JsonPropertyName("totalMembers")]
    public int TotalMembers { get; set; }

    /// <summary>
    /// Wöchentliches Ziel erreicht?
    /// </summary>
    [JsonIgnore]
    public bool IsWeeklyGoalReached => WeeklyProgress >= WeeklyGoal;

    /// <summary>
    /// Fortschritt zum Wochenziel (0-1).
    /// </summary>
    [JsonIgnore]
    public double WeeklyGoalProgress => WeeklyGoal > 0
        ? Math.Clamp((double)(WeeklyProgress / WeeklyGoal), 0.0, 1.0) : 0.0;

    /// <summary>
    /// Einkommens-Bonus durch Gilden-Level (+1% pro Level, max 20%).
    /// </summary>
    [JsonIgnore]
    public decimal IncomeBonus => Math.Min(0.20m, Level * 0.01m);

    /// <summary>
    /// Alle verfügbaren Innungen mit Leaderboard-IDs.
    /// </summary>
    public static List<GuildDefinition> GetAvailableGuilds() =>
    [
        new("guild_wood", "GuildWoodworkers", "Hammer", "#8B4513", "TODO_GUILD_WOOD"),
        new("guild_metal", "GuildMetalworkers", "Wrench", "#757575", "TODO_GUILD_METAL"),
        new("guild_electric", "GuildElectricians", "LightningBolt", "#FFC107", "TODO_GUILD_ELECTRIC"),
        new("guild_build", "GuildBuilders", "OfficeBuildingCog", "#795548", "TODO_GUILD_BUILD"),
        new("guild_design", "GuildDesigners", "Pencil", "#4CAF50", "TODO_GUILD_DESIGN"),
    ];

    /// <summary>
    /// Erstellt eine neue Gilde (ohne simulierte Mitglieder).
    /// </summary>
    public static Guild Create(string guildId, int playerLevel)
    {
        var guildDef = GetAvailableGuilds().FirstOrDefault(g => g.Id == guildId);

        return new Guild
        {
            Id = guildId,
            NameKey = guildDef?.NameKey ?? guildId,
            LeaderboardId = guildDef?.LeaderboardId ?? "",
            WeeklyGoal = Math.Max(100_000m, playerLevel * 10_000m),
            LastWeeklyReset = GetLastMonday()
        };
    }

    private static DateTime GetLastMonday()
    {
        var today = DateTime.UtcNow.Date;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }
}

/// <summary>
/// Definition einer verfügbaren Gilde (statisch).
/// </summary>
public record GuildDefinition(string Id, string NameKey, string Icon, string Color, string LeaderboardId);
