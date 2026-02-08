using System.Text.Json;
using BomberBlast.Models;
using Material.Icons;
using MeineApps.Core.Ava.Services;

namespace BomberBlast.Services;

/// <summary>
/// Shop-Verwaltung mit persistenten Upgrades via IPreferencesService
/// </summary>
public class ShopService : IShopService
{
    private const string UPGRADES_KEY = "PlayerUpgrades";
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly IPreferencesService _preferences;
    private readonly ICoinService _coinService;
    private PlayerUpgrades _upgrades;

    public PlayerUpgrades Upgrades => _upgrades;

    public ShopService(IPreferencesService preferences, ICoinService coinService)
    {
        _preferences = preferences;
        _coinService = coinService;
        _upgrades = Load();
    }

    public List<ShopDisplayItem> GetShopItems()
    {
        return
        [
            CreateItem(UpgradeType.StartBombs, "UpgradeStartBombs", "UpgradeStartBombsDesc",
                MaterialIconKind.Bomb, "#FF6B35"),
            CreateItem(UpgradeType.StartFire, "UpgradeStartFire", "UpgradeStartFireDesc",
                MaterialIconKind.Fire, "#FF4444"),
            CreateItem(UpgradeType.StartSpeed, "UpgradeStartSpeed", "UpgradeStartSpeedDesc",
                MaterialIconKind.FlashOutline, "#FFD700"),
            CreateItem(UpgradeType.ExtraLives, "UpgradeExtraLives", "UpgradeExtraLivesDesc",
                MaterialIconKind.Heart, "#E91E63"),
            CreateItem(UpgradeType.ScoreMultiplier, "UpgradeScoreMultiplier", "UpgradeScoreMultiplierDesc",
                MaterialIconKind.Star, "#9C27B0"),
            CreateItem(UpgradeType.TimeBonus, "UpgradeTimeBonus", "UpgradeTimeBonusDesc",
                MaterialIconKind.ClockFast, "#00BCD4")
        ];
    }

    private ShopDisplayItem CreateItem(UpgradeType type, string nameKey, string descKey,
        MaterialIconKind icon, string iconColor)
    {
        int level = _upgrades.GetLevel(type);
        int maxLevel = PlayerUpgrades.GetMaxLevel(type);
        bool isMaxed = level >= maxLevel;
        int nextPrice = _upgrades.GetNextPrice(type);

        return new ShopDisplayItem
        {
            Type = type,
            NameKey = nameKey,
            DescriptionKey = descKey,
            IconKind = icon,
            IconColor = iconColor,
            MaxLevel = maxLevel,
            CurrentLevel = level,
            NextPrice = nextPrice,
            IsMaxed = isMaxed,
            CanAfford = !isMaxed && _coinService.CanAfford(nextPrice),
            LevelText = $"{level}/{maxLevel}"
        };
    }

    public bool TryPurchase(UpgradeType type)
    {
        if (_upgrades.IsMaxed(type))
            return false;

        int price = _upgrades.GetNextPrice(type);
        if (price <= 0)
            return false;

        if (!_coinService.TrySpendCoins(price))
            return false;

        _upgrades.Upgrade(type);
        Save();
        return true;
    }

    public float GetScoreMultiplier() => _upgrades.GetScoreMultiplier();
    public int GetTimeBonusMultiplier() => _upgrades.GetTimeBonusMultiplier();
    public int GetStartBombs() => _upgrades.GetStartBombs();
    public int GetStartFire() => _upgrades.GetStartFire();
    public bool HasStartSpeed() => _upgrades.HasStartSpeed();
    public int GetStartLives(bool isArcade) => _upgrades.GetStartLives(isArcade);

    public void ResetUpgrades()
    {
        _upgrades.Reset();
        Save();
    }

    private PlayerUpgrades Load()
    {
        try
        {
            string json = _preferences.Get<string>(UPGRADES_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<PlayerUpgrades>(json, JsonOptions) ?? new PlayerUpgrades();
            }
        }
        catch
        {
            // Fehler beim Laden → Standardwerte
        }
        return new PlayerUpgrades();
    }

    private void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_upgrades, JsonOptions);
            _preferences.Set(UPGRADES_KEY, json);
        }
        catch
        {
            // Speichern fehlgeschlagen
        }
    }
}
