using System.Text.Json.Serialization;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Models;

/// <summary>
/// The complete game state, persisted between sessions.
/// This is the single source of truth for all game data.
/// </summary>
public class GameState
{
    /// <summary>
    /// Version of the save format (for migration support).
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the game was first started.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the game was last saved.
    /// </summary>
    [JsonPropertyName("lastSavedAt")]
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the player last played (for offline progress).
    /// </summary>
    [JsonPropertyName("lastPlayedAt")]
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // PLAYER PROGRESS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current player level (determines workshop unlocks).
    /// </summary>
    [JsonPropertyName("playerLevel")]
    public int PlayerLevel { get; set; } = 1;

    /// <summary>
    /// Current XP towards next level.
    /// </summary>
    [JsonPropertyName("currentXp")]
    public int CurrentXp { get; set; }

    /// <summary>
    /// Total XP earned (lifetime).
    /// </summary>
    [JsonPropertyName("totalXp")]
    public int TotalXp { get; set; }

    /// <summary>
    /// Current money balance.
    /// </summary>
    [JsonPropertyName("money")]
    public decimal Money { get; set; } = 100m; // Start with 100€

    /// <summary>
    /// Total money earned (lifetime).
    /// </summary>
    [JsonPropertyName("totalMoneyEarned")]
    public decimal TotalMoneyEarned { get; set; }

    /// <summary>
    /// Total money spent on upgrades and workers.
    /// </summary>
    [JsonPropertyName("totalMoneySpent")]
    public decimal TotalMoneySpent { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // WORKSHOPS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// All workshops in the game.
    /// </summary>
    [JsonPropertyName("workshops")]
    public List<Workshop> Workshops { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════════════
    // ORDERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Currently available orders.
    /// </summary>
    [JsonPropertyName("availableOrders")]
    public List<Order> AvailableOrders { get; set; } = [];

    /// <summary>
    /// Order currently in progress (if any).
    /// </summary>
    [JsonPropertyName("activeOrder")]
    public Order? ActiveOrder { get; set; }

    /// <summary>
    /// Total orders completed (lifetime).
    /// </summary>
    [JsonPropertyName("totalOrdersCompleted")]
    public int TotalOrdersCompleted { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // STATISTICS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total mini-games played.
    /// </summary>
    [JsonPropertyName("totalMiniGamesPlayed")]
    public int TotalMiniGamesPlayed { get; set; }

    /// <summary>
    /// Total perfect ratings achieved.
    /// </summary>
    [JsonPropertyName("perfectRatings")]
    public int PerfectRatings { get; set; }

    /// <summary>
    /// Current streak of perfect ratings.
    /// </summary>
    [JsonPropertyName("perfectStreak")]
    public int PerfectStreak { get; set; }

    /// <summary>
    /// Best streak of perfect ratings.
    /// </summary>
    [JsonPropertyName("bestPerfectStreak")]
    public int BestPerfectStreak { get; set; }

    /// <summary>
    /// Total time played in seconds.
    /// </summary>
    [JsonPropertyName("totalPlayTimeSeconds")]
    public long TotalPlayTimeSeconds { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // PRESTIGE (Endgame)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Number of times prestiged.
    /// </summary>
    [JsonPropertyName("prestigeLevel")]
    public int PrestigeLevel { get; set; }

    /// <summary>
    /// Permanent income multiplier from prestige.
    /// </summary>
    [JsonPropertyName("prestigeMultiplier")]
    public decimal PrestigeMultiplier { get; set; } = 1.0m;

    // ═══════════════════════════════════════════════════════════════════════
    // SETTINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether sound effects are enabled.
    /// </summary>
    [JsonPropertyName("soundEnabled")]
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Whether music is enabled.
    /// </summary>
    [JsonPropertyName("musicEnabled")]
    public bool MusicEnabled { get; set; } = true;

    /// <summary>
    /// Whether haptic feedback is enabled.
    /// </summary>
    [JsonPropertyName("hapticsEnabled")]
    public bool HapticsEnabled { get; set; } = true;

    /// <summary>
    /// Selected language code (en, de, es, fr, it, pt).
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    // ═══════════════════════════════════════════════════════════════════════
    // PREMIUM STATUS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether premium (ad-free) was purchased.
    /// </summary>
    [JsonPropertyName("isPremium")]
    public bool IsPremium { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // DAILY REWARDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When the daily reward was last claimed.
    /// </summary>
    [JsonPropertyName("lastDailyRewardClaim")]
    public DateTime LastDailyRewardClaim { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Current daily reward streak (consecutive days).
    /// </summary>
    [JsonPropertyName("dailyRewardStreak")]
    public int DailyRewardStreak { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // BOOSTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When the 2x speed boost expires.
    /// </summary>
    [JsonPropertyName("speedBoostEndTime")]
    public DateTime SpeedBoostEndTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// When the 50% XP boost expires.
    /// </summary>
    [JsonPropertyName("xpBoostEndTime")]
    public DateTime XpBoostEndTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Whether speed boost is currently active.
    /// </summary>
    [JsonIgnore]
    public bool IsSpeedBoostActive => SpeedBoostEndTime > DateTime.UtcNow;

    /// <summary>
    /// Whether XP boost is currently active.
    /// </summary>
    [JsonIgnore]
    public bool IsXpBoostActive => XpBoostEndTime > DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // ACHIEVEMENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// List of unlocked achievement IDs.
    /// </summary>
    [JsonPropertyName("unlockedAchievements")]
    public List<string> UnlockedAchievements { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════════════
    // TUTORIAL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the tutorial has been completed.
    /// </summary>
    [JsonPropertyName("tutorialCompleted")]
    public bool TutorialCompleted { get; set; }

    /// <summary>
    /// Current tutorial step (0-based).
    /// </summary>
    [JsonPropertyName("tutorialStep")]
    public int TutorialStep { get; set; }

    /// <summary>
    /// Maximum offline hours for non-premium users.
    /// </summary>
    [JsonIgnore]
    public int MaxOfflineHours => IsPremium ? 8 : 2;

    // ═══════════════════════════════════════════════════════════════════════
    // CALCULATED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// XP required for next level.
    /// </summary>
    [JsonIgnore]
    public int XpForNextLevel => CalculateXpForLevel(PlayerLevel + 1);

    /// <summary>
    /// Progress towards next level (0.0 - 1.0).
    /// </summary>
    [JsonIgnore]
    public double LevelProgress
    {
        get
        {
            int required = XpForNextLevel - CalculateXpForLevel(PlayerLevel);
            int current = CurrentXp - CalculateXpForLevel(PlayerLevel);
            return Math.Clamp((double)current / required, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Total income per second from all workshops.
    /// </summary>
    [JsonIgnore]
    public decimal TotalIncomePerSecond
    {
        get
        {
            decimal total = Workshops.Sum(w => w.IncomePerSecond);
            return total * PrestigeMultiplier;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculates total XP needed to reach a level.
    /// </summary>
    public static int CalculateXpForLevel(int level)
    {
        // XP curve: Level 2 = 100, Level 10 = ~2500, Level 50 = ~40000 (was 1.5, now 1.2 for smoother progression)
        if (level <= 1) return 0;
        return (int)(100 * Math.Pow(level - 1, 1.2));
    }

    /// <summary>
    /// Adds XP and handles level-ups.
    /// Returns the number of level-ups that occurred.
    /// </summary>
    public int AddXp(int amount)
    {
        CurrentXp += amount;
        TotalXp += amount;

        int levelUps = 0;
        while (CurrentXp >= XpForNextLevel)
        {
            PlayerLevel++;
            levelUps++;
        }

        return levelUps;
    }

    /// <summary>
    /// Gets a workshop by type, creating it if it doesn't exist.
    /// </summary>
    public Workshop GetOrCreateWorkshop(WorkshopType type)
    {
        var workshop = Workshops.FirstOrDefault(w => w.Type == type);
        if (workshop == null)
        {
            workshop = Workshop.Create(type);
            Workshops.Add(workshop);
        }
        return workshop;
    }

    /// <summary>
    /// Checks if a workshop type is unlocked at the current player level.
    /// </summary>
    public bool IsWorkshopUnlocked(WorkshopType type)
    {
        return PlayerLevel >= type.GetUnlockLevel();
    }

    /// <summary>
    /// Creates a new game state with default values.
    /// </summary>
    public static GameState CreateNew()
    {
        var state = new GameState();

        // Create the starting workshop (Carpenter) with 1 worker
        var carpenter = Workshop.Create(WorkshopType.Carpenter);
        carpenter.Workers.Add(Worker.CreateRandom());
        state.Workshops.Add(carpenter);

        return state;
    }
}
