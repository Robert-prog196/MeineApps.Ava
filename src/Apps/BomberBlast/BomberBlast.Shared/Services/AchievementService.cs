using System.Text.Json;
using BomberBlast.Models;
using BomberBlast.Models.Entities;
using MeineApps.Core.Ava.Services;

namespace BomberBlast.Services;

/// <summary>
/// Achievement-Service: ~20 Achievements in 5 Kategorien.
/// Persistiert Fortschritt via IPreferencesService (JSON).
/// </summary>
public class AchievementService : IAchievementService
{
    private const string ACHIEVEMENTS_KEY = "Achievements";
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly IPreferencesService _preferences;
    private readonly ICoinService _coinService;
    private readonly IPlayGamesService _playGames;
    private readonly List<Achievement> _achievements;
    private AchievementData _data;

    public event EventHandler<Achievement>? AchievementUnlocked;

    public IReadOnlyList<Achievement> Achievements => _achievements;
    public int UnlockedCount => _achievements.Count(a => a.IsUnlocked);
    public int TotalCount => _achievements.Count;
    public int TotalEnemyKills => _data.TotalEnemyKills;

    public AchievementService(IPreferencesService preferences, ICoinService coinService, IPlayGamesService playGames)
    {
        _preferences = preferences;
        _coinService = coinService;
        _playGames = playGames;
        _achievements = CreateAchievements();
        _data = Load();
        ApplyProgress();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EVENT HANDLER
    // ═══════════════════════════════════════════════════════════════════════

    public Achievement? OnLevelCompleted(int level, int score, int stars, int bombsUsed, float timeRemaining, float timeUsed, bool noDamage)
    {
        Achievement? newUnlock = null;

        // Erstes Level abgeschlossen
        newUnlock ??= TryUnlock("first_victory");

        // Fortschritts-Achievements: Welten abschließen
        if (level == 10) newUnlock ??= TryUnlock("world1");
        if (level == 20) newUnlock ??= TryUnlock("world2");
        if (level == 30) newUnlock ??= TryUnlock("world3");
        if (level == 40) newUnlock ??= TryUnlock("world4");
        if (level == 50) newUnlock ??= TryUnlock("world5");

        // Geschick: Level ohne Treffer
        if (noDamage) newUnlock ??= TryUnlock("no_damage");

        // Geschick: ≤3 Bomben
        if (bombsUsed <= 3) newUnlock ??= TryUnlock("efficient");

        // Geschick: Level in unter 60 Sekunden abgeschlossen
        if (timeUsed > 0 && timeUsed <= 60f) newUnlock ??= TryUnlock("speedrun");

        return newUnlock;
    }

    public Achievement? OnEnemyKilled(int totalKills)
    {
        // Kampf-Achievements: kumulativer Kill-Zähler
        _data.TotalEnemyKills = totalKills;
        Save();

        Achievement? newUnlock = null;

        // Fortschritt aktualisieren
        UpdateProgress("kills_100", totalKills);
        UpdateProgress("kills_500", totalKills);
        UpdateProgress("kills_1000", totalKills);

        if (totalKills >= 100) newUnlock ??= TryUnlock("kills_100");
        if (totalKills >= 500) newUnlock ??= TryUnlock("kills_500");
        if (totalKills >= 1000) newUnlock ??= TryUnlock("kills_1000");

        return newUnlock;
    }

    public Achievement? OnArcadeWaveReached(int wave)
    {
        if (wave > _data.HighestArcadeWave)
        {
            _data.HighestArcadeWave = wave;
            Save();
        }

        UpdateProgress("arcade_10", wave);
        UpdateProgress("arcade_25", wave);

        Achievement? newUnlock = null;
        if (wave >= 10) newUnlock ??= TryUnlock("arcade_10");
        if (wave >= 25) newUnlock ??= TryUnlock("arcade_25");

        return newUnlock;
    }

    public Achievement? OnStarsUpdated(int totalStars)
    {
        _data.TotalStars = totalStars;
        Save();

        // Sterne ans Leaderboard senden
        _ = _playGames.SubmitScoreAsync(PlayGamesIds.LeaderboardTotalStars, totalStars);

        UpdateProgress("stars_50", totalStars);
        UpdateProgress("stars_100", totalStars);
        UpdateProgress("stars_150", totalStars);

        Achievement? newUnlock = null;
        if (totalStars >= 50) newUnlock ??= TryUnlock("stars_50");
        if (totalStars >= 100) newUnlock ??= TryUnlock("stars_100");
        if (totalStars >= 150) newUnlock ??= TryUnlock("stars_150");

        return newUnlock;
    }

    public Achievement? OnComboReached(int comboCount)
    {
        Achievement? newUnlock = null;
        if (comboCount >= 3) newUnlock ??= TryUnlock("combo3");
        if (comboCount >= 5) newUnlock ??= TryUnlock("combo5");
        return newUnlock;
    }

    public Achievement? OnBombKicked()
    {
        _data.TotalBombsKicked++;
        Save();

        UpdateProgress("kick_master", _data.TotalBombsKicked);
        if (_data.TotalBombsKicked >= 25) return TryUnlock("kick_master");
        return null;
    }

    public Achievement? OnPowerBombUsed()
    {
        _data.TotalPowerBombs++;
        Save();

        UpdateProgress("power_bomber", _data.TotalPowerBombs);
        if (_data.TotalPowerBombs >= 10) return TryUnlock("power_bomber");
        return null;
    }

    public Achievement? OnCurseSurvived(CurseType curseType)
    {
        // Bit-Flag: Jeden überlebten Curse-Typ merken
        int curseFlag = curseType switch
        {
            CurseType.Diarrhea => 1,
            CurseType.Slow => 2,
            CurseType.Constipation => 4,
            CurseType.ReverseControls => 8,
            _ => 0
        };

        if (curseFlag == 0) return null;

        _data.CurseTypesSurvived |= curseFlag;
        Save();

        // Zähle gesetzte Bits
        int survived = 0;
        int flags = _data.CurseTypesSurvived;
        while (flags > 0)
        {
            survived += flags & 1;
            flags >>= 1;
        }

        UpdateProgress("curse_survivor", survived);
        if (survived >= 4) return TryUnlock("curse_survivor");
        return null;
    }

    public Achievement? OnDailyChallengeCompleted(int totalCompleted, int currentStreak)
    {
        Achievement? newUnlock = null;

        UpdateProgress("daily_streak7", currentStreak);
        UpdateProgress("daily_complete30", totalCompleted);

        if (currentStreak >= 7) newUnlock ??= TryUnlock("daily_streak7");
        if (totalCompleted >= 30) newUnlock ??= TryUnlock("daily_complete30");

        return newUnlock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════════════════

    private Achievement? TryUnlock(string id)
    {
        var achievement = _achievements.Find(a => a.Id == id);
        if (achievement == null || achievement.IsUnlocked)
            return null;

        achievement.IsUnlocked = true;
        achievement.Progress = achievement.Target;
        _data.UnlockedIds.Add(id);
        Save();

        // Coin-Belohnung gutschreiben
        if (achievement.CoinReward > 0)
            _coinService.AddCoins(achievement.CoinReward);

        // Google Play Games Achievement freischalten
        var gpgsId = PlayGamesIds.GetGpgsAchievementId(id);
        if (gpgsId != null)
            _ = _playGames.UnlockAchievementAsync(gpgsId);

        // Event feuern für Toast-Anzeige
        AchievementUnlocked?.Invoke(this, achievement);

        return achievement;
    }

    private void UpdateProgress(string id, int progress)
    {
        var achievement = _achievements.Find(a => a.Id == id);
        if (achievement != null && !achievement.IsUnlocked)
        {
            achievement.Progress = Math.Min(progress, achievement.Target);
        }
    }

    private void ApplyProgress()
    {
        // Unlock-Status wiederherstellen
        foreach (var id in _data.UnlockedIds)
        {
            var achievement = _achievements.Find(a => a.Id == id);
            if (achievement != null)
            {
                achievement.IsUnlocked = true;
                achievement.Progress = achievement.Target;
            }
        }

        // Fortschritt aktualisieren
        UpdateProgress("kills_100", _data.TotalEnemyKills);
        UpdateProgress("kills_500", _data.TotalEnemyKills);
        UpdateProgress("kills_1000", _data.TotalEnemyKills);
        UpdateProgress("arcade_10", _data.HighestArcadeWave);
        UpdateProgress("arcade_25", _data.HighestArcadeWave);
        UpdateProgress("stars_50", _data.TotalStars);
        UpdateProgress("stars_100", _data.TotalStars);
        UpdateProgress("stars_150", _data.TotalStars);
        UpdateProgress("kick_master", _data.TotalBombsKicked);
        UpdateProgress("power_bomber", _data.TotalPowerBombs);

        // Curse-Survivor: Überlebte Typen zählen
        int survived = 0;
        int curseFlags = _data.CurseTypesSurvived;
        while (curseFlags > 0) { survived += curseFlags & 1; curseFlags >>= 1; }
        UpdateProgress("curse_survivor", survived);
    }

    private static List<Achievement> CreateAchievements()
    {
        return
        [
            // Fortschritt (5) - Coins steigen mit Welt-Schwierigkeit
            new() { Id = "world1", NameKey = "AchWorld1", DescriptionKey = "AchWorld1Desc", Category = AchievementCategory.Progress, Target = 1, IconName = "Star", CoinReward = 500 },
            new() { Id = "world2", NameKey = "AchWorld2", DescriptionKey = "AchWorld2Desc", Category = AchievementCategory.Progress, Target = 1, IconName = "Star", CoinReward = 750 },
            new() { Id = "world3", NameKey = "AchWorld3", DescriptionKey = "AchWorld3Desc", Category = AchievementCategory.Progress, Target = 1, IconName = "Star", CoinReward = 1000 },
            new() { Id = "world4", NameKey = "AchWorld4", DescriptionKey = "AchWorld4Desc", Category = AchievementCategory.Progress, Target = 1, IconName = "Star", CoinReward = 1500 },
            new() { Id = "world5", NameKey = "AchWorld5", DescriptionKey = "AchWorld5Desc", Category = AchievementCategory.Progress, Target = 1, IconName = "Crown", CoinReward = 3000 },

            // Meisterschaft (3)
            new() { Id = "stars_50", NameKey = "AchStars50", DescriptionKey = "AchStars50Desc", Category = AchievementCategory.Mastery, Target = 50, IconName = "StarCircle", CoinReward = 1000 },
            new() { Id = "stars_100", NameKey = "AchStars100", DescriptionKey = "AchStars100Desc", Category = AchievementCategory.Mastery, Target = 100, IconName = "StarCircle", CoinReward = 2000 },
            new() { Id = "stars_150", NameKey = "AchStars150", DescriptionKey = "AchStars150Desc", Category = AchievementCategory.Mastery, Target = 150, IconName = "StarShooting", CoinReward = 5000 },

            // Kampf (3)
            new() { Id = "kills_100", NameKey = "AchKills100", DescriptionKey = "AchKills100Desc", Category = AchievementCategory.Combat, Target = 100, IconName = "Sword", CoinReward = 500 },
            new() { Id = "kills_500", NameKey = "AchKills500", DescriptionKey = "AchKills500Desc", Category = AchievementCategory.Combat, Target = 500, IconName = "SwordCross", CoinReward = 1500 },
            new() { Id = "kills_1000", NameKey = "AchKills1000", DescriptionKey = "AchKills1000Desc", Category = AchievementCategory.Combat, Target = 1000, IconName = "Skull", CoinReward = 3000 },

            // Geschick (3)
            new() { Id = "no_damage", NameKey = "AchNoDamage", DescriptionKey = "AchNoDamageDesc", Category = AchievementCategory.Skill, Target = 1, IconName = "Shield", CoinReward = 1000 },
            new() { Id = "efficient", NameKey = "AchEfficient", DescriptionKey = "AchEfficientDesc", Category = AchievementCategory.Skill, Target = 1, IconName = "Target", CoinReward = 1000 },
            new() { Id = "speedrun", NameKey = "AchSpeedrun", DescriptionKey = "AchSpeedrunDesc", Category = AchievementCategory.Skill, Target = 1, IconName = "TimerSand", CoinReward = 1500 },

            // Arcade (2)
            new() { Id = "arcade_10", NameKey = "AchArcade10", DescriptionKey = "AchArcade10Desc", Category = AchievementCategory.Arcade, Target = 10, IconName = "Fire", CoinReward = 1500 },
            new() { Id = "arcade_25", NameKey = "AchArcade25", DescriptionKey = "AchArcade25Desc", Category = AchievementCategory.Arcade, Target = 25, IconName = "Flash", CoinReward = 3000 },

            // ═══ Neue Achievements (8) ═══

            // Fortschritt: Erstes Level abgeschlossen
            new() { Id = "first_victory", NameKey = "AchFirstVictory", DescriptionKey = "AchFirstVictoryDesc", Category = AchievementCategory.Progress, Target = 1, IconName = "Flag", CoinReward = 200 },

            // Fortschritt: Daily Challenge Streak + Total
            new() { Id = "daily_streak7", NameKey = "AchDailyStreak7", DescriptionKey = "AchDailyStreak7Desc", Category = AchievementCategory.Progress, Target = 7, IconName = "CalendarCheck", CoinReward = 2000 },
            new() { Id = "daily_complete30", NameKey = "AchDailyComplete30", DescriptionKey = "AchDailyComplete30Desc", Category = AchievementCategory.Progress, Target = 30, IconName = "CalendarStar", CoinReward = 5000 },

            // Geschick: Combo x3 und x5
            new() { Id = "combo3", NameKey = "AchCombo3", DescriptionKey = "AchCombo3Desc", Category = AchievementCategory.Skill, Target = 1, IconName = "Flash", CoinReward = 500 },
            new() { Id = "combo5", NameKey = "AchCombo5", DescriptionKey = "AchCombo5Desc", Category = AchievementCategory.Skill, Target = 1, IconName = "FlashAlert", CoinReward = 2000 },

            // Geschick: Alle 4 Curse-Typen überlebt
            new() { Id = "curse_survivor", NameKey = "AchCurseSurvivor", DescriptionKey = "AchCurseSurvivorDesc", Category = AchievementCategory.Skill, Target = 4, IconName = "Skull", CoinReward = 1500 },

            // Kampf: 25 Bomben gekickt
            new() { Id = "kick_master", NameKey = "AchKickMaster", DescriptionKey = "AchKickMasterDesc", Category = AchievementCategory.Combat, Target = 25, IconName = "ShoeSneaker", CoinReward = 1000 },

            // Kampf: 10 Power-Bombs eingesetzt
            new() { Id = "power_bomber", NameKey = "AchPowerBomber", DescriptionKey = "AchPowerBomberDesc", Category = AchievementCategory.Combat, Target = 10, IconName = "Bomb", CoinReward = 1500 },
        ];
    }

    private AchievementData Load()
    {
        try
        {
            string json = _preferences.Get<string>(ACHIEVEMENTS_KEY, "");
            if (!string.IsNullOrEmpty(json))
                return JsonSerializer.Deserialize<AchievementData>(json, JsonOptions) ?? new AchievementData();
        }
        catch { /* Standardwerte */ }
        return new AchievementData();
    }

    private void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_data, JsonOptions);
            _preferences.Set(ACHIEVEMENTS_KEY, json);
        }
        catch { /* Speichern fehlgeschlagen */ }
    }

    private class AchievementData
    {
        public HashSet<string> UnlockedIds { get; set; } = [];
        public int TotalEnemyKills { get; set; }
        public int HighestArcadeWave { get; set; }
        public int TotalStars { get; set; }
        public int TotalBombsKicked { get; set; }
        public int TotalPowerBombs { get; set; }
        /// <summary>Bit-Flags: 1=Diarrhea, 2=Slow, 4=Constipation, 8=ReverseControls</summary>
        public int CurseTypesSurvived { get; set; }
    }
}
