using System.Text.Json.Serialization;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Models;

/// <summary>
/// Represents an achievement in the game.
/// </summary>
public class Achievement
{
    /// <summary>
    /// Unique identifier for the achievement.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// Localization key for the title.
    /// </summary>
    [JsonPropertyName("titleKey")]
    public string TitleKey { get; set; } = "";

    /// <summary>
    /// Fallback title in English.
    /// </summary>
    [JsonPropertyName("titleFallback")]
    public string TitleFallback { get; set; } = "";

    /// <summary>
    /// Localization key for the description.
    /// </summary>
    [JsonPropertyName("descriptionKey")]
    public string DescriptionKey { get; set; } = "";

    /// <summary>
    /// Fallback description in English.
    /// </summary>
    [JsonPropertyName("descriptionFallback")]
    public string DescriptionFallback { get; set; } = "";

    /// <summary>
    /// Category of the achievement.
    /// </summary>
    [JsonPropertyName("category")]
    public AchievementCategory Category { get; set; }

    /// <summary>
    /// Icon/emoji for the achievement.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "\ud83c\udfc6";

    /// <summary>
    /// Target value to unlock the achievement.
    /// </summary>
    [JsonPropertyName("targetValue")]
    public int TargetValue { get; set; } = 1;

    /// <summary>
    /// Current progress value.
    /// </summary>
    [JsonPropertyName("currentValue")]
    public int CurrentValue { get; set; }

    /// <summary>
    /// Money reward for unlocking.
    /// </summary>
    [JsonPropertyName("moneyReward")]
    public decimal MoneyReward { get; set; }

    /// <summary>
    /// XP reward for unlocking.
    /// </summary>
    [JsonPropertyName("xpReward")]
    public int XpReward { get; set; }

    /// <summary>
    /// Whether this achievement has been unlocked.
    /// </summary>
    [JsonPropertyName("isUnlocked")]
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// When the achievement was unlocked.
    /// </summary>
    [JsonPropertyName("unlockedAt")]
    public DateTime? UnlockedAt { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [JsonIgnore]
    public double Progress => TargetValue > 0 ? Math.Min(100.0, (double)CurrentValue / TargetValue * 100) : 0;

    /// <summary>
    /// Progress as a fraction (0.0-1.0).
    /// </summary>
    [JsonIgnore]
    public double ProgressFraction => Progress / 100.0;

    /// <summary>
    /// Whether the achievement is close to being unlocked (>75%).
    /// </summary>
    [JsonIgnore]
    public bool IsCloseToUnlock => !IsUnlocked && Progress >= 75;
}

/// <summary>
/// Predefined achievements for the game.
/// </summary>
public static class Achievements
{
    public static List<Achievement> GetAll()
    {
        return
        [
            // Orders Category
            new() { Id = "first_order", TitleKey = "AchFirstOrder", TitleFallback = "First Steps", DescriptionKey = "AchFirstOrderDesc", DescriptionFallback = "Complete your first order", Category = AchievementCategory.Orders, Icon = "\ud83d\udce6", TargetValue = 1, MoneyReward = 100, XpReward = 25 },
            new() { Id = "orders_10", TitleKey = "AchOrders10", TitleFallback = "Getting Started", DescriptionKey = "AchOrders10Desc", DescriptionFallback = "Complete 10 orders", Category = AchievementCategory.Orders, Icon = "\ud83d\udce6", TargetValue = 10, MoneyReward = 500, XpReward = 100 },
            new() { Id = "orders_50", TitleKey = "AchOrders50", TitleFallback = "Reliable Worker", DescriptionKey = "AchOrders50Desc", DescriptionFallback = "Complete 50 orders", Category = AchievementCategory.Orders, Icon = "\ud83d\udce6", TargetValue = 50, MoneyReward = 2500, XpReward = 300 },
            new() { Id = "orders_100", TitleKey = "AchOrders100", TitleFallback = "Master Craftsman", DescriptionKey = "AchOrders100Desc", DescriptionFallback = "Complete 100 orders", Category = AchievementCategory.Orders, Icon = "\ud83c\udfc6", TargetValue = 100, MoneyReward = 5000, XpReward = 500 },
            new() { Id = "orders_500", TitleKey = "AchOrders500", TitleFallback = "Industry Legend", DescriptionKey = "AchOrders500Desc", DescriptionFallback = "Complete 500 orders", Category = AchievementCategory.Orders, Icon = "\ud83d\udc51", TargetValue = 500, MoneyReward = 25000, XpReward = 1000 },

            // Mini-Games Category
            new() { Id = "perfect_first", TitleKey = "AchPerfectFirst", TitleFallback = "Perfection!", DescriptionKey = "AchPerfectFirstDesc", DescriptionFallback = "Get your first Perfect rating", Category = AchievementCategory.MiniGames, Icon = "\u2b50", TargetValue = 1, MoneyReward = 200, XpReward = 50 },
            new() { Id = "perfect_10", TitleKey = "AchPerfect10", TitleFallback = "Skilled Hands", DescriptionKey = "AchPerfect10Desc", DescriptionFallback = "Get 10 Perfect ratings", Category = AchievementCategory.MiniGames, Icon = "\u2b50", TargetValue = 10, MoneyReward = 1000, XpReward = 150 },
            new() { Id = "perfect_50", TitleKey = "AchPerfect50", TitleFallback = "Precision Master", DescriptionKey = "AchPerfect50Desc", DescriptionFallback = "Get 50 Perfect ratings", Category = AchievementCategory.MiniGames, Icon = "\u2b50", TargetValue = 50, MoneyReward = 5000, XpReward = 400 },
            new() { Id = "streak_5", TitleKey = "AchStreak5", TitleFallback = "On Fire!", DescriptionKey = "AchStreak5Desc", DescriptionFallback = "Get 5 Perfect ratings in a row", Category = AchievementCategory.MiniGames, Icon = "\ud83d\udd25", TargetValue = 5, MoneyReward = 500, XpReward = 100 },
            new() { Id = "streak_10", TitleKey = "AchStreak10", TitleFallback = "Unstoppable", DescriptionKey = "AchStreak10Desc", DescriptionFallback = "Get 10 Perfect ratings in a row", Category = AchievementCategory.MiniGames, Icon = "\ud83d\udd25", TargetValue = 10, MoneyReward = 2000, XpReward = 300 },
            new() { Id = "games_100", TitleKey = "AchGames100", TitleFallback = "Mini-Game Veteran", DescriptionKey = "AchGames100Desc", DescriptionFallback = "Play 100 mini-games", Category = AchievementCategory.MiniGames, Icon = "\ud83c\udfae", TargetValue = 100, MoneyReward = 2500, XpReward = 250 },

            // Workshops Category
            new() { Id = "workshop_level10", TitleKey = "AchWorkshop10", TitleFallback = "Upgraded", DescriptionKey = "AchWorkshop10Desc", DescriptionFallback = "Upgrade any workshop to level 10", Category = AchievementCategory.Workshops, Icon = "\ud83d\udd27", TargetValue = 10, MoneyReward = 1000, XpReward = 200 },
            new() { Id = "workshop_level25", TitleKey = "AchWorkshop25", TitleFallback = "Expert Facility", DescriptionKey = "AchWorkshop25Desc", DescriptionFallback = "Upgrade any workshop to level 25", Category = AchievementCategory.Workshops, Icon = "\ud83d\udd27", TargetValue = 25, MoneyReward = 5000, XpReward = 500 },
            new() { Id = "all_workshops", TitleKey = "AchAllWorkshops", TitleFallback = "Full House", DescriptionKey = "AchAllWorkshopsDesc", DescriptionFallback = "Unlock all 6 workshops", Category = AchievementCategory.Workshops, Icon = "\ud83c\udfed", TargetValue = 6, MoneyReward = 2500, XpReward = 400 },
            new() { Id = "worker_first", TitleKey = "AchWorkerFirst", TitleFallback = "Team Builder", DescriptionKey = "AchWorkerFirstDesc", DescriptionFallback = "Hire your first worker", Category = AchievementCategory.Workshops, Icon = "\ud83d\udc77", TargetValue = 1, MoneyReward = 100, XpReward = 25 },
            new() { Id = "workers_10", TitleKey = "AchWorkers10", TitleFallback = "Growing Team", DescriptionKey = "AchWorkers10Desc", DescriptionFallback = "Hire 10 workers total", Category = AchievementCategory.Workshops, Icon = "\ud83d\udc77", TargetValue = 10, MoneyReward = 1000, XpReward = 200 },
            new() { Id = "workers_25", TitleKey = "AchWorkers25", TitleFallback = "Big Business", DescriptionKey = "AchWorkers25Desc", DescriptionFallback = "Hire 25 workers total", Category = AchievementCategory.Workshops, Icon = "\ud83d\udc77", TargetValue = 25, MoneyReward = 5000, XpReward = 500 },

            // Money Category
            new() { Id = "money_1k", TitleKey = "AchMoney1k", TitleFallback = "First Thousand", DescriptionKey = "AchMoney1kDesc", DescriptionFallback = "Earn 1,000\u20ac total", Category = AchievementCategory.Money, Icon = "\ud83d\udcb0", TargetValue = 1000, MoneyReward = 100, XpReward = 25 },
            new() { Id = "money_10k", TitleKey = "AchMoney10k", TitleFallback = "Making Money", DescriptionKey = "AchMoney10kDesc", DescriptionFallback = "Earn 10,000\u20ac total", Category = AchievementCategory.Money, Icon = "\ud83d\udcb0", TargetValue = 10000, MoneyReward = 500, XpReward = 100 },
            new() { Id = "money_100k", TitleKey = "AchMoney100k", TitleFallback = "Wealthy", DescriptionKey = "AchMoney100kDesc", DescriptionFallback = "Earn 100,000\u20ac total", Category = AchievementCategory.Money, Icon = "\ud83d\udcb0", TargetValue = 100000, MoneyReward = 2500, XpReward = 300 },
            new() { Id = "money_1m", TitleKey = "AchMoney1m", TitleFallback = "Millionaire", DescriptionKey = "AchMoney1mDesc", DescriptionFallback = "Earn 1,000,000\u20ac total", Category = AchievementCategory.Money, Icon = "\ud83e\udd11", TargetValue = 1000000, MoneyReward = 10000, XpReward = 500 },

            // Time Category
            new() { Id = "play_1h", TitleKey = "AchPlay1h", TitleFallback = "Dedicated", DescriptionKey = "AchPlay1hDesc", DescriptionFallback = "Play for 1 hour total", Category = AchievementCategory.Time, Icon = "\u23f0", TargetValue = 3600, MoneyReward = 250, XpReward = 50 },
            new() { Id = "play_10h", TitleKey = "AchPlay10h", TitleFallback = "Committed", DescriptionKey = "AchPlay10hDesc", DescriptionFallback = "Play for 10 hours total", Category = AchievementCategory.Time, Icon = "\u23f0", TargetValue = 36000, MoneyReward = 2500, XpReward = 250 },
            new() { Id = "daily_7", TitleKey = "AchDaily7", TitleFallback = "Week Warrior", DescriptionKey = "AchDaily7Desc", DescriptionFallback = "Log in 7 days in a row", Category = AchievementCategory.Time, Icon = "\ud83d\udcc5", TargetValue = 7, MoneyReward = 1000, XpReward = 150 },

            // Special Category
            new() { Id = "level_10", TitleKey = "AchLevel10", TitleFallback = "Rising Star", DescriptionKey = "AchLevel10Desc", DescriptionFallback = "Reach level 10", Category = AchievementCategory.Special, Icon = "\ud83c\udf1f", TargetValue = 10, MoneyReward = 1000, XpReward = 200 },
            new() { Id = "level_25", TitleKey = "AchLevel25", TitleFallback = "Experienced", DescriptionKey = "AchLevel25Desc", DescriptionFallback = "Reach level 25", Category = AchievementCategory.Special, Icon = "\ud83c\udf1f", TargetValue = 25, MoneyReward = 5000, XpReward = 500 },
            new() { Id = "level_50", TitleKey = "AchLevel50", TitleFallback = "Veteran", DescriptionKey = "AchLevel50Desc", DescriptionFallback = "Reach level 50", Category = AchievementCategory.Special, Icon = "\ud83d\udc51", TargetValue = 50, MoneyReward = 10000, XpReward = 1000 },
            new() { Id = "prestige_1", TitleKey = "AchPrestige1", TitleFallback = "New Beginning", DescriptionKey = "AchPrestige1Desc", DescriptionFallback = "Prestige for the first time", Category = AchievementCategory.Special, Icon = "\u2728", TargetValue = 1, MoneyReward = 5000, XpReward = 500 }
        ];
    }
}
