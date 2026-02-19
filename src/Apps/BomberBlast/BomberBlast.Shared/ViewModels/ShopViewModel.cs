using System.Collections.ObjectModel;
using Avalonia.Media;
using BomberBlast.Models;
using BomberBlast.Models.Entities;
using BomberBlast.Models.Levels;
using BomberBlast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Premium.Ava.Services;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel fuer den Shop - zeigt Upgrades, PowerUp-Übersicht, Skins und Coin-Stand.
/// Implementiert IDisposable fuer BalanceChanged-Unsubscription.
/// </summary>
public partial class ShopViewModel : ObservableObject, IDisposable
{
    private readonly IShopService _shopService;
    private readonly ICoinService _coinService;
    private readonly ILocalizationService _localizationService;
    private readonly IProgressService _progressService;
    private readonly ICustomizationService _customizationService;
    private readonly IPurchaseService _purchaseService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public event Action<string>? NavigationRequested;
    public event Action<string, string>? MessageRequested;

    /// <summary>Kauf erfolgreich (Upgrade-Name)</summary>
    public event Action<string>? PurchaseSucceeded;

    /// <summary>Zu wenig Coins</summary>
    public event Action? InsufficientFunds;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<ShopDisplayItem> _shopItems = [];

    [ObservableProperty]
    private ObservableCollection<PowerUpDisplayItem> _powerUpItems = [];

    [ObservableProperty]
    private ObservableCollection<PowerUpDisplayItem> _mechanicItems = [];

    [ObservableProperty]
    private ObservableCollection<SkinDisplayItem> _skinItems = [];

    [ObservableProperty]
    private string _coinsText = "0";

    [ObservableProperty]
    private int _coinBalance;

    [ObservableProperty]
    private string _shopTitleText = "";

    [ObservableProperty]
    private string _sectionStartUpgradesText = "";

    [ObservableProperty]
    private string _sectionScoreBoosterText = "";

    [ObservableProperty]
    private string _sectionPowerUpsText = "";

    [ObservableProperty]
    private string _sectionMechanicsText = "";

    [ObservableProperty]
    private string _sectionSkinsText = "";

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public ShopViewModel(IShopService shopService, ICoinService coinService,
        ILocalizationService localizationService, IProgressService progressService,
        ICustomizationService customizationService, IPurchaseService purchaseService)
    {
        _shopService = shopService;
        _coinService = coinService;
        _localizationService = localizationService;
        _progressService = progressService;
        _customizationService = customizationService;
        _purchaseService = purchaseService;

        _coinService.BalanceChanged += OnBalanceChanged;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    public void OnAppearing()
    {
        UpdateLocalizedTexts();
        RefreshItems();
        RefreshPowerUpItems();
        RefreshMechanicItems();
        RefreshSkinItems();
        UpdateCoinDisplay();
    }

    public void UpdateLocalizedTexts()
    {
        ShopTitleText = _localizationService.GetString("ShopTitle");
        SectionStartUpgradesText = _localizationService.GetString("SectionStartUpgrades");
        SectionScoreBoosterText = _localizationService.GetString("SectionScoreBooster");
        SectionPowerUpsText = _localizationService.GetString("SectionPowerUps");
        SectionMechanicsText = _localizationService.GetString("SectionMechanics");
        SectionSkinsText = _localizationService.GetString("SectionSkins") ?? _localizationService.GetString("SkinsTitle");
    }

    private void RefreshItems()
    {
        var items = _shopService.GetShopItems();

        // Namen und Beschreibungen lokalisieren
        foreach (var item in items)
        {
            item.DisplayName = _localizationService.GetString(item.NameKey);
            item.DisplayDescription = _localizationService.GetString(item.DescriptionKey);
            item.LevelText = string.Format(
                _localizationService.GetString("UpgradeLevelFormat"),
                item.CurrentLevel, item.MaxLevel);
            item.Refresh(_coinService.Balance);
        }

        ShopItems = new ObservableCollection<ShopDisplayItem>(items);
    }

    private void RefreshPowerUpItems()
    {
        var items = new List<PowerUpDisplayItem>();
        foreach (PowerUpType type in Enum.GetValues<PowerUpType>())
        {
            items.Add(CreateDisplayItem(
                "powerup_" + type.ToString().ToLower(),
                $"PowerUp_{type}",
                type.GetUnlockLevel(),
                GetPowerUpIcon(type),
                GetPowerUpAvaloniaColor(type)));
        }
        PowerUpItems = new ObservableCollection<PowerUpDisplayItem>(items);
    }

    private void RefreshMechanicItems()
    {
        var mechanics = new[] { WorldMechanic.Ice, WorldMechanic.Conveyor, WorldMechanic.Teleporter, WorldMechanic.LavaCrack };
        var items = new List<PowerUpDisplayItem>();
        foreach (var mech in mechanics)
        {
            items.Add(CreateDisplayItem(
                "mechanic_" + mech.ToString().ToLower(),
                $"Mechanic_{mech}",
                mech.GetUnlockLevel(),
                GetMechanicIcon(mech),
                GetMechanicColor(mech)));
        }
        MechanicItems = new ObservableCollection<PowerUpDisplayItem>(items);
    }

    /// <summary>Erstellt ein PowerUpDisplayItem mit Unlock-Status-Logik</summary>
    private PowerUpDisplayItem CreateDisplayItem(string id, string nameKey, int unlockLevel,
        MaterialIconKind icon, Color color)
    {
        int highest = _progressService.HighestCompletedLevel;
        bool isUnlocked = highest >= unlockLevel || unlockLevel <= 1;
        var unlockedFormat = _localizationService.GetString("UnlockedAt") ?? "Ab Level {0}";
        var unlockedText = _localizationService.GetString("Unlocked") ?? "Freigeschaltet";

        return new PowerUpDisplayItem
        {
            Id = id,
            DisplayName = _localizationService.GetString(nameKey) ?? nameKey,
            DisplayDescription = isUnlocked
                ? (_localizationService.GetString(nameKey + "_Desc") ?? "")
                : "???",
            IconKind = icon,
            IconColor = isUnlocked ? color : Color.Parse("#666666"),
            UnlockLevel = unlockLevel,
            IsUnlocked = isUnlocked,
            UnlockText = isUnlocked ? unlockedText : string.Format(unlockedFormat, unlockLevel)
        };
    }

    private void RefreshSkinItems()
    {
        var currentSkin = _customizationService.PlayerSkin;
        bool isPremium = _purchaseService.IsPremium;
        var equippedText = _localizationService.GetString("SkinEquipped") ?? "Equipped";
        var lockedText = _localizationService.GetString("SkinLocked") ?? _localizationService.GetString("SkinPremiumOnly") ?? "Premium Only";
        var selectText = _localizationService.GetString("SkinSelect") ?? "Select";

        var items = new List<SkinDisplayItem>();
        foreach (var skin in _customizationService.AvailablePlayerSkins)
        {
            bool isEquipped = skin.Id == currentSkin.Id;
            bool isLocked = skin.IsPremiumOnly && !isPremium;

            items.Add(new SkinDisplayItem
            {
                Id = skin.Id,
                DisplayName = _localizationService.GetString(skin.NameKey) ?? skin.Id,
                PrimaryColor = Color.FromRgb(skin.PrimaryColor.Red, skin.PrimaryColor.Green, skin.PrimaryColor.Blue),
                SecondaryColor = Color.FromRgb(skin.SecondaryColor.Red, skin.SecondaryColor.Green, skin.SecondaryColor.Blue),
                IsPremiumOnly = skin.IsPremiumOnly,
                HasGlow = skin.GlowColor.HasValue,
                IsEquipped = isEquipped,
                IsLocked = isLocked,
                StatusText = isEquipped ? equippedText : (isLocked ? lockedText : selectText)
            });
        }
        SkinItems = new ObservableCollection<SkinDisplayItem>(items);
    }

    private void UpdateCoinDisplay()
    {
        CoinBalance = _coinService.Balance;
        CoinsText = _coinService.Balance.ToString("N0");
    }

    private void OnBalanceChanged(object? sender, EventArgs e)
    {
        UpdateCoinDisplay();
        foreach (var item in ShopItems)
        {
            item.Refresh(_coinService.Balance);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void Purchase(ShopDisplayItem? item)
    {
        if (item == null || item.IsMaxed) return;

        if (!_coinService.CanAfford(item.NextPrice))
        {
            InsufficientFunds?.Invoke();
            return;
        }

        if (_shopService.TryPurchase(item.Type))
        {
            var upgradeName = _localizationService.GetString(item.NameKey);
            PurchaseSucceeded?.Invoke(upgradeName ?? item.NameKey);
            RefreshItems();
            UpdateCoinDisplay();
        }
    }

    [RelayCommand]
    private void SelectSkin(SkinDisplayItem? item)
    {
        if (item == null || item.IsLocked || item.IsEquipped) return;

        _customizationService.SetPlayerSkin(item.Id);
        RefreshSkinItems();

        var skinName = _localizationService.GetString(
            PlayerSkins.All.FirstOrDefault(s => s.Id == item.Id)?.NameKey ?? "") ?? item.Id;
        PurchaseSucceeded?.Invoke(skinName);
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    public void Dispose()
    {
        _coinService.BalanceChanged -= OnBalanceChanged;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ICON/FARB-MAPPING (konsistent mit GameRenderer/HelpIconRenderer)
    // ═══════════════════════════════════════════════════════════════════════

    private static MaterialIconKind GetPowerUpIcon(PowerUpType type) => type switch
    {
        PowerUpType.BombUp => MaterialIconKind.Bomb,
        PowerUpType.Fire => MaterialIconKind.Fire,
        PowerUpType.Speed => MaterialIconKind.FlashOutline,
        PowerUpType.Wallpass => MaterialIconKind.Ghost,
        PowerUpType.Detonator => MaterialIconKind.FlashAlert,
        PowerUpType.Bombpass => MaterialIconKind.ArrowRightBoldCircleOutline,
        PowerUpType.Flamepass => MaterialIconKind.ShieldOutline,
        PowerUpType.Mystery => MaterialIconKind.HelpCircleOutline,
        PowerUpType.Kick => MaterialIconKind.ShoeSneaker,
        PowerUpType.LineBomb => MaterialIconKind.DotsHorizontal,
        PowerUpType.PowerBomb => MaterialIconKind.StarCircle,
        PowerUpType.Skull => MaterialIconKind.SkullOutline,
        _ => MaterialIconKind.HelpCircleOutline
    };

    private static Color GetPowerUpAvaloniaColor(PowerUpType type) => type switch
    {
        PowerUpType.BombUp => Color.Parse("#5050F0"),
        PowerUpType.Fire => Color.Parse("#F05A28"),
        PowerUpType.Speed => Color.Parse("#3CDC50"),
        PowerUpType.Wallpass => Color.Parse("#966432"),
        PowerUpType.Detonator => Color.Parse("#F02828"),
        PowerUpType.Bombpass => Color.Parse("#323296"),
        PowerUpType.Flamepass => Color.Parse("#F0BE28"),
        PowerUpType.Mystery => Color.Parse("#B450F0"),
        PowerUpType.Kick => Color.Parse("#FFA500"),
        PowerUpType.LineBomb => Color.Parse("#00B4FF"),
        PowerUpType.PowerBomb => Color.Parse("#FF3232"),
        PowerUpType.Skull => Color.Parse("#640064"),
        _ => Colors.White
    };

    private static MaterialIconKind GetMechanicIcon(WorldMechanic mech) => mech switch
    {
        WorldMechanic.Ice => MaterialIconKind.Snowflake,
        WorldMechanic.Conveyor => MaterialIconKind.ArrowRightBold,
        WorldMechanic.Teleporter => MaterialIconKind.SwapHorizontalBold,
        WorldMechanic.LavaCrack => MaterialIconKind.Terrain,
        _ => MaterialIconKind.HelpCircleOutline
    };

    private static Color GetMechanicColor(WorldMechanic mech) => mech switch
    {
        WorldMechanic.Ice => Color.Parse("#64C8FF"),
        WorldMechanic.Conveyor => Color.Parse("#A0A0A0"),
        WorldMechanic.Teleporter => Color.Parse("#C864FF"),
        WorldMechanic.LavaCrack => Color.Parse("#FF5000"),
        _ => Colors.White
    };
}
