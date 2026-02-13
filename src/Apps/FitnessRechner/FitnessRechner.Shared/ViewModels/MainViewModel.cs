using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;
using FitnessRechner.Services;
using FitnessRechner.ViewModels.Calculators;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessRechner.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private readonly IPurchaseService _purchaseService;
    private readonly IAdService _adService;
    private readonly ITrackingService _trackingService;
    private readonly IFoodSearchService _foodSearchService;
    private readonly IPreferencesService _preferences;
    private readonly ILocalizationService _localization;
    private readonly IRewardedAdService _rewardedAdService;
    private readonly StreakService _streakService;

    /// <summary>
    /// Raised when the VM wants to show a message (title, message).
    /// </summary>
    [ObservableProperty]
    private bool _isAdBannerVisible;

    public event Action<string, string>? MessageRequested;

    /// <summary>
    /// Floating Text anzeigen (text, category).
    /// </summary>
    public event Action<string, string>? FloatingTextRequested;

    /// <summary>
    /// Confetti-Celebration ausloesen.
    /// </summary>
    public event Action? CelebrationRequested;

    public MainViewModel(
        IPurchaseService purchaseService,
        IAdService adService,
        IRewardedAdService rewardedAdService,
        ITrackingService trackingService,
        IFoodSearchService foodSearchService,
        IPreferencesService preferences,
        ILocalizationService localization,
        IThemeService themeService,
        StreakService streakService,
        SettingsViewModel settingsViewModel,
        ProgressViewModel progressViewModel,
        FoodSearchViewModel foodSearchViewModel)
    {
        _purchaseService = purchaseService;
        _adService = adService;
        _rewardedAdService = rewardedAdService;
        _trackingService = trackingService;
        _foodSearchService = foodSearchService;
        _preferences = preferences;
        _localization = localization;
        _streakService = streakService;

        _rewardedAdService.AdUnavailable += OnAdUnavailable;

        IsAdBannerVisible = _adService.BannerVisible;
        _adService.AdsStateChanged += OnAdsStateChanged;

        // Banner beim Start anzeigen (fuer Desktop + Fallback falls AdMobHelper fehlschlaegt)
        if (_adService.AdsEnabled && !_purchaseService.IsPremium)
            _adService.ShowBanner();

        SettingsViewModel = settingsViewModel;
        ProgressViewModel = progressViewModel;
        FoodSearchViewModel = foodSearchViewModel;

        // Game Juice Events vom ProgressViewModel weiterleiten
        progressViewModel.FloatingTextRequested += OnProgressFloatingText;
        progressViewModel.CelebrationRequested += OnProgressCelebration;

        // FoodSearchViewModel Navigation verdrahten (Barcode-Scanner)
        foodSearchViewModel.NavigationRequested += OnFoodSearchNavigation;

        _purchaseService.PremiumStatusChanged += OnPremiumStatusChanged;
        settingsViewModel.LanguageChanged += OnLanguageChanged;

        // Streak bei jeder Logging-Aktivität aktualisieren
        _trackingService.EntryAdded += RecordStreakActivity;
        _foodSearchService.FoodLogAdded += RecordStreakActivity;
    }

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(NavHomeText));
        OnPropertyChanged(nameof(NavProgressText));
        OnPropertyChanged(nameof(NavFoodText));
        OnPropertyChanged(nameof(NavSettingsText));
        OnPropertyChanged(nameof(AppDescription));
        OnPropertyChanged(nameof(CalcBmiLabel));
        OnPropertyChanged(nameof(CalcCaloriesLabel));
        OnPropertyChanged(nameof(CalcWaterLabel));
        OnPropertyChanged(nameof(CalcIdealWeightLabel));
        OnPropertyChanged(nameof(CalcBodyFatLabel));
        OnPropertyChanged(nameof(CalculatorsLabel));
        OnPropertyChanged(nameof(MyProgressLabel));
        OnPropertyChanged(nameof(RemoveAdsText));
        OnPropertyChanged(nameof(PremiumPriceText));
        OnPropertyChanged(nameof(SectionCalculatorsText));
        OnPropertyChanged(nameof(StreakTitleText));
        OnPropertyChanged(nameof(GreetingText));
        OnPropertyChanged(nameof(QuickAddWeightLabel));
        UpdateStreakDisplay();

        // Aktiven Calculator aktualisieren falls nötig
        if (CurrentCalculatorVm is CaloriesViewModel cal)
            cal.UpdateLocalizedTexts();
    }

    #region Tab Navigation

    [ObservableProperty]
    private int _selectedTab;

    public bool IsHomeActive => SelectedTab == 0;
    public bool IsProgressActive => SelectedTab == 1;
    public bool IsFoodActive => SelectedTab == 2;
    public bool IsSettingsActive => SelectedTab == 3;

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsProgressActive));
        OnPropertyChanged(nameof(IsFoodActive));
        OnPropertyChanged(nameof(IsSettingsActive));

        // Daten für ausgewählten Tab laden
        if (value == 1)
            _ = ProgressViewModel.OnAppearingAsync();
        else if (value == 2)
            FoodSearchViewModel.OnAppearing();
    }

    [RelayCommand]
    private void SelectHomeTab() { CurrentPage = null; SelectedTab = 0; }

    [RelayCommand]
    private void SelectProgressTab() { CurrentPage = null; SelectedTab = 1; }

    [RelayCommand]
    private void SelectFoodTab() { CurrentPage = null; SelectedTab = 2; }

    [RelayCommand]
    private void SelectSettingsTab() { CurrentPage = null; SelectedTab = 3; }

    // Tab text properties from localization
    public string NavHomeText => _localization.GetString("TodayDashboard");
    public string NavProgressText => _localization.GetString("Progress");
    public string NavFoodText => _localization.GetString("FoodSearch");
    public string NavSettingsText => _localization.GetString("SettingsTitle");

    #endregion

    #region Child ViewModels

    public SettingsViewModel SettingsViewModel { get; }
    public ProgressViewModel ProgressViewModel { get; }
    public FoodSearchViewModel FoodSearchViewModel { get; }

    #endregion

    #region Premium Status

    [ObservableProperty]
    private bool _isPremium;

    #endregion

    #region Dashboard Properties

    [ObservableProperty]
    private string _todayWeightDisplay = "-";

    [ObservableProperty]
    private string _todayBmiDisplay = "-";

    [ObservableProperty]
    private string _todayWaterDisplay = "-";

    [ObservableProperty]
    private string _todayCaloriesDisplay = "-";

    [ObservableProperty]
    private bool _hasDashboardData;

    // Fortschritt (0-100)
    [ObservableProperty]
    private double _calorieProgress;

    [ObservableProperty]
    private double _waterProgress;

    // ProgressBar nur anzeigen wenn Wert > 0 (vermeidet sichtbare Track-Striche bei 0)
    public bool HasWaterProgress => WaterProgress > 0;
    public bool HasCalorieProgress => CalorieProgress > 0;

    partial void OnWaterProgressChanged(double value) => OnPropertyChanged(nameof(HasWaterProgress));
    partial void OnCalorieProgressChanged(double value) => OnPropertyChanged(nameof(HasCalorieProgress));

    // Quick-Add Properties
    [ObservableProperty]
    private bool _showWeightQuickAdd;

    [ObservableProperty]
    private double _quickAddWeight = 70.0;

    private bool _wasWaterGoalReachedOnDashboard;

    // Streak
    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private string _streakDisplay = "";

    [ObservableProperty]
    private string _streakBestDisplay = "";

    [ObservableProperty]
    private bool _hasStreak;

    #endregion

    #region Localized Labels

    public string AppDescription => _localization.GetString("AppDescription");
    public string CalcBmiLabel => _localization.GetString("CalcBmi");
    public string CalcCaloriesLabel => _localization.GetString("CalcCalories");
    public string CalcWaterLabel => _localization.GetString("CalcWater");
    public string CalcIdealWeightLabel => _localization.GetString("CalcIdealWeight");
    public string CalcBodyFatLabel => _localization.GetString("CalcBodyFat");
    public string CalculatorsLabel => _localization.GetString("Calculators");
    public string MyProgressLabel => _localization.GetString("MyProgress");

    // Design-Redesign Properties
    public string RemoveAdsText => _localization.GetString("RemoveAds") ?? "Go Ad-Free";
    public string PremiumPriceText => _localization.GetString("PremiumPrice") ?? "From 3.99 €";
    public string SectionCalculatorsText => _localization.GetString("SectionCalculators") ?? "Calculators";
    public string StreakTitleText => _localization.GetString("StreakTitle") ?? "Logging Streak";
    public string QuickAddWeightLabel => _localization.GetString("QuickAddWeight") ?? "Enter weight";

    // Tageszeit-Begrüßung
    public string GreetingText
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var key = hour switch
            {
                < 12 => "GreetingMorning",
                < 18 => "GreetingAfternoon",
                _ => "GreetingEvening"
            };
            return _localization.GetString(key) ?? "Hello!";
        }
    }

    #endregion

    public async Task OnAppearingAsync()
    {
        IsPremium = _purchaseService.IsPremium;
        UpdateStreakDisplay();
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Streak-Aktivität registrieren (wird von Child-VMs aufgerufen).
    /// </summary>
    public void RecordStreakActivity()
    {
        var wasLoggedToday = _streakService.IsLoggedToday;
        var isMilestone = _streakService.RecordActivity();
        UpdateStreakDisplay();

        if (isMilestone)
        {
            // Meilenstein → Confetti + spezielle Nachricht
            var milestone = _streakService.CurrentStreak;
            var text = string.Format(_localization.GetString("StreakMilestone") ?? "{0} day streak!", milestone);
            FloatingTextRequested?.Invoke(text, "streak");
            CelebrationRequested?.Invoke();
        }
        else if (!wasLoggedToday && _streakService.IsLoggedToday)
        {
            // Erster Log des Tages (kein Meilenstein) → einfaches Feedback
            var streak = _streakService.CurrentStreak;
            var text = string.Format(_localization.GetString("StreakIncreased") ?? "+1! {0} day streak", streak);
            FloatingTextRequested?.Invoke(text, "streak");
        }
    }

    private void UpdateStreakDisplay()
    {
        CurrentStreak = _streakService.CurrentStreak;
        HasStreak = CurrentStreak > 0;

        if (CurrentStreak == 0)
        {
            StreakDisplay = _localization.GetString("StreakStart") ?? "Start your streak!";
        }
        else if (CurrentStreak == 1)
        {
            StreakDisplay = _localization.GetString("StreakDay") ?? "1 day";
        }
        else
        {
            StreakDisplay = string.Format(_localization.GetString("StreakDays") ?? "{0} days", CurrentStreak);
        }

        var best = _streakService.BestStreak;
        StreakBestDisplay = string.Format(_localization.GetString("StreakBest") ?? "Best: {0}", best);
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            // Zurücksetzen damit veraltete Daten nicht fälschlich "true" halten
            HasDashboardData = false;

            // Weight
            var weightEntry = await _trackingService.GetLatestEntryAsync(TrackingType.Weight);
            if (weightEntry != null && weightEntry.Date.Date >= DateTime.Today.AddDays(-7))
            {
                TodayWeightDisplay = $"{weightEntry.Value:F1}";
                HasDashboardData = true;
            }
            else
            {
                TodayWeightDisplay = "-";
            }

            // BMI
            var bmiEntry = await _trackingService.GetLatestEntryAsync(TrackingType.Bmi);
            if (bmiEntry != null && bmiEntry.Date.Date >= DateTime.Today.AddDays(-7))
            {
                TodayBmiDisplay = $"{bmiEntry.Value:F1}";
                HasDashboardData = true;
            }
            else
            {
                TodayBmiDisplay = "-";
            }

            // Water (today only)
            var waterEntry = await _trackingService.GetLatestEntryAsync(TrackingType.Water);
            var waterGoal = _preferences.Get(PreferenceKeys.WaterGoal, 2500.0);
            if (waterEntry != null && waterEntry.Date.Date == DateTime.Today)
            {
                var progress = waterGoal > 0 ? (waterEntry.Value / waterGoal) * 100 : 0;
                TodayWaterDisplay = $"{progress:F0}%";
                WaterProgress = Math.Min(progress, 100);
                HasDashboardData = true;
            }
            else
            {
                TodayWaterDisplay = "0%";
                WaterProgress = 0;
            }

            // Calories (today only)
            var summary = await _foodSearchService.GetDailySummaryAsync(DateTime.Today);
            var calorieGoal = _preferences.Get(PreferenceKeys.CalorieGoal, 2000.0);
            if (summary.TotalCalories > 0 || calorieGoal > 0)
            {
                TodayCaloriesDisplay = $"{summary.TotalCalories:F0}";
                CalorieProgress = calorieGoal > 0
                    ? Math.Min((summary.TotalCalories / calorieGoal) * 100, 100)
                    : 0;
                HasDashboardData = true;
            }
            else
            {
                TodayCaloriesDisplay = "-";
                CalorieProgress = 0;
            }
        }
        catch (Exception)
        {
            // Silently handle - dashboard will show default values
        }
    }

    #region Calculator Page Navigation

    [ObservableProperty]
    private string? _currentPage;

    [ObservableProperty]
    private ObservableObject? _currentCalculatorVm;

    public bool IsCalculatorOpen => CurrentPage != null;

    partial void OnCurrentPageChanged(string? value)
    {
        OnPropertyChanged(nameof(IsCalculatorOpen));

        // Alte Events abmelden bevor neues VM erstellt wird
        CleanupCurrentCalculatorVm();

        if (value != null)
        {
            CurrentCalculatorVm = CreateCalculatorVm(value);
        }
        else
        {
            CurrentCalculatorVm = null;
        }
    }

    /// <summary>
    /// Meldet Event-Handler des aktuellen Calculator-VMs ab um Speicherlecks zu vermeiden.
    /// </summary>
    private void CleanupCurrentCalculatorVm()
    {
        switch (CurrentCalculatorVm)
        {
            case BmiViewModel bmi:
                bmi.NavigationRequested -= OnCalculatorGoBack;
                bmi.MessageRequested -= OnCalculatorMessage;
                break;
            case CaloriesViewModel cal:
                cal.NavigationRequested -= OnCalculatorGoBack;
                cal.MessageRequested -= OnCalculatorMessage;
                break;
            case WaterViewModel water:
                water.NavigationRequested -= OnCalculatorGoBack;
                water.MessageRequested -= OnCalculatorMessage;
                break;
            case IdealWeightViewModel iw:
                iw.NavigationRequested -= OnCalculatorGoBack;
                iw.MessageRequested -= OnCalculatorMessage;
                break;
            case BodyFatViewModel bf:
                bf.NavigationRequested -= OnCalculatorGoBack;
                bf.MessageRequested -= OnCalculatorMessage;
                break;
            case BarcodeScannerViewModel scanner:
                scanner.NavigationRequested -= OnCalculatorGoBack;
                scanner.FoodSelected -= OnFoodSelectedFromScanner;
                scanner.Dispose();
                break;
        }
    }

    private ObservableObject? CreateCalculatorVm(string page)
    {
        // Route-Parameter parsen (z.B. "BarcodeScannerPage?barcode=1234567890")
        string? barcodeParam = null;
        var basePage = page;
        var queryIndex = page.IndexOf('?');
        if (queryIndex >= 0)
        {
            basePage = page[..queryIndex];
            var query = page[(queryIndex + 1)..];
            foreach (var param in query.Split('&'))
            {
                var parts = param.Split('=', 2);
                if (parts.Length == 2 && parts[0] == "barcode")
                    barcodeParam = Uri.UnescapeDataString(parts[1]);
            }
        }

        ObservableObject? vm = basePage switch
        {
            "BmiPage" => App.Services.GetRequiredService<BmiViewModel>(),
            "CaloriesPage" => App.Services.GetRequiredService<CaloriesViewModel>(),
            "WaterPage" => App.Services.GetRequiredService<WaterViewModel>(),
            "IdealWeightPage" => App.Services.GetRequiredService<IdealWeightViewModel>(),
            "BodyFatPage" => App.Services.GetRequiredService<BodyFatViewModel>(),
            "BarcodeScannerPage" => App.Services.GetRequiredService<BarcodeScannerViewModel>(),
            _ => null
        };

        // Wire navigation and message events
        switch (vm)
        {
            case BmiViewModel bmi:
                bmi.NavigationRequested += OnCalculatorGoBack;
                bmi.MessageRequested += OnCalculatorMessage;
                break;
            case CaloriesViewModel cal:
                cal.NavigationRequested += OnCalculatorGoBack;
                cal.MessageRequested += OnCalculatorMessage;
                break;
            case WaterViewModel water:
                water.NavigationRequested += OnCalculatorGoBack;
                water.MessageRequested += OnCalculatorMessage;
                break;
            case IdealWeightViewModel iw:
                iw.NavigationRequested += OnCalculatorGoBack;
                iw.MessageRequested += OnCalculatorMessage;
                break;
            case BodyFatViewModel bf:
                bf.NavigationRequested += OnCalculatorGoBack;
                bf.MessageRequested += OnCalculatorMessage;
                break;
            case BarcodeScannerViewModel scanner:
                scanner.NavigationRequested += OnCalculatorGoBack;
                scanner.FoodSelected += OnFoodSelectedFromScanner;
                // Barcode-Parameter vorhanden → direkt API-Lookup starten
                if (!string.IsNullOrEmpty(barcodeParam))
                    _ = scanner.OnBarcodeDetected(barcodeParam);
                break;
        }

        return vm;
    }

    private void OnCalculatorGoBack(string route)
    {
        if (route == "..")
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentPage = null;
                // Refresh dashboard after calculator use
                _ = LoadDashboardDataAsync();
            });
        }
    }

    private void OnCalculatorMessage(string title, string message)
    {
        MessageRequested?.Invoke(title, message);
    }

    private void OnFoodSearchNavigation(string route)
    {
        // Barcode-Scanner und andere FoodSearch-Navigationen behandeln
        if (route.StartsWith("BarcodeScannerPage"))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentPage = route;
            });
        }
        else if (route == "..")
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentPage = null;
            });
        }
    }

    private void OnFoodSelectedFromScanner(FoodItem food)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Zurueck zum Food-Tab und gescanntes Lebensmittel anwenden
            CurrentPage = null;
            SelectedTab = 2;
            FoodSearchViewModel.ApplyParameters(new Dictionary<string, object>
            {
                { "SelectedFood", food }
            });
        });
    }

    #endregion

    /// <summary>
    /// Versucht eine Ebene zurückzunavigieren (Overlays schließen, Sub-Views verlassen).
    /// Gibt true zurück wenn etwas geschlossen wurde, false wenn bereits auf Root-Ebene.
    /// </summary>
    public bool TryGoBack()
    {
        // 1. Weight Quick-Add Panel offen → schließen
        if (ShowWeightQuickAdd)
        {
            ShowWeightQuickAdd = false;
            return true;
        }

        // 2. Calculator-Page offen → schließen
        if (CurrentPage != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => CurrentPage = null);
            return true;
        }

        // 2. ProgressVM Overlays prüfen
        if (SelectedTab == 1)
        {
            var vm = ProgressViewModel;
            if (vm.ShowAnalysisOverlay) { vm.ShowAnalysisOverlay = false; return true; }
            if (vm.ShowAnalysisAdOverlay) { vm.ShowAnalysisAdOverlay = false; return true; }
            if (vm.ShowExportAdOverlay) { vm.ShowExportAdOverlay = false; return true; }
            if (vm.ShowFoodSearch) { vm.ShowFoodSearch = false; return true; }
            if (vm.ShowAddForm) { vm.ShowAddForm = false; return true; }
            if (vm.ShowAddFoodPanel) { vm.ShowAddFoodPanel = false; return true; }
        }

        // 3. Nicht auf Home-Tab → zum Home-Tab wechseln
        if (SelectedTab != 0)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => SelectedTab = 0);
            return true;
        }

        // 4. Auf Root-Ebene → false (App kann beendet werden)
        return false;
    }

    [RelayCommand]
    private void OpenSettings() => SelectedTab = 3;

    [RelayCommand]
    private void OpenBmi() => CurrentPage = "BmiPage";

    [RelayCommand]
    private void OpenCalories() => CurrentPage = "CaloriesPage";

    [RelayCommand]
    private void OpenWater() => CurrentPage = "WaterPage";

    [RelayCommand]
    private void OpenIdealWeight() => CurrentPage = "IdealWeightPage";

    [RelayCommand]
    private void OpenBodyFat() => CurrentPage = "BodyFatPage";

    [RelayCommand]
    private void OpenProgress() { CurrentPage = null; SelectedTab = 1; }

    #region Dashboard Quick-Add

    [RelayCommand]
    private void OpenWeightQuickAdd()
    {
        // Letztes Gewicht laden als Startwert
        _ = LoadLastWeightAsync();
        ShowWeightQuickAdd = true;
    }

    private async Task LoadLastWeightAsync()
    {
        try
        {
            var lastWeight = await _trackingService.GetLatestEntryAsync(FitnessRechner.Models.TrackingType.Weight);
            if (lastWeight != null)
                QuickAddWeight = lastWeight.Value;
        }
        catch { /* Standardwert beibehalten */ }
    }

    [RelayCommand]
    private async Task SaveWeightQuickAdd()
    {
        try
        {
            if (QuickAddWeight < 20 || QuickAddWeight > 500) return;

            var entry = new FitnessRechner.Models.TrackingEntry
            {
                Type = FitnessRechner.Models.TrackingType.Weight,
                Value = QuickAddWeight,
                Date = DateTime.Today
            };
            await _trackingService.AddEntryAsync(entry);

            ShowWeightQuickAdd = false;
            FloatingTextRequested?.Invoke($"+{QuickAddWeight:F1} kg", "info");
            await LoadDashboardDataAsync();
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(
                _localization.GetString("Error") ?? "Error",
                _localization.GetString("ErrorSavingData") ?? "Error saving data");
        }
    }

    [RelayCommand]
    private void CancelWeightQuickAdd() => ShowWeightQuickAdd = false;

    [RelayCommand]
    private async Task QuickAddWater(string amountStr)
    {
        if (!int.TryParse(amountStr, out var amount)) return;
        try
        {
            var today = await _trackingService.GetLatestEntryAsync(FitnessRechner.Models.TrackingType.Water);

            if (today != null && today.Date.Date == DateTime.Today)
            {
                today.Value += amount;
                await _trackingService.UpdateEntryAsync(today);
            }
            else
            {
                var entry = new FitnessRechner.Models.TrackingEntry
                {
                    Type = FitnessRechner.Models.TrackingType.Water,
                    Value = amount,
                    Date = DateTime.Today
                };
                await _trackingService.AddEntryAsync(entry);
            }

            FloatingTextRequested?.Invoke($"+{amount} ml", "info");
            await LoadDashboardDataAsync();

            // Wasser-Ziel Celebration (einmal pro Session)
            if (!_wasWaterGoalReachedOnDashboard && WaterProgress >= 100)
            {
                _wasWaterGoalReachedOnDashboard = true;
                FloatingTextRequested?.Invoke(
                    _localization.GetString("GoalReached") ?? "Goal reached!", "success");
                CelebrationRequested?.Invoke();
            }
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(
                _localization.GetString("Error") ?? "Error",
                _localization.GetString("ErrorSavingData") ?? "Error saving data");
        }
    }

    [RelayCommand]
    private void OpenFoodQuickAdd()
    {
        CurrentPage = null;
        SelectedTab = 2;
        // Quick-Add Panel im FoodSearch öffnen
        FoodSearchViewModel.ShowQuickAddPanel = true;
    }

    #endregion

    private void OnAdUnavailable() =>
        MessageRequested?.Invoke(AppStrings.AdVideoNotAvailableTitle, AppStrings.AdVideoNotAvailableMessage);

    private void OnAdsStateChanged(object? sender, EventArgs e) =>
        IsAdBannerVisible = _adService.BannerVisible;

    private void OnProgressFloatingText(string text, string category) =>
        FloatingTextRequested?.Invoke(text, category);

    private void OnProgressCelebration() =>
        CelebrationRequested?.Invoke();

    private void OnPremiumStatusChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsPremium = _purchaseService.IsPremium;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _purchaseService.PremiumStatusChanged -= OnPremiumStatusChanged;
        SettingsViewModel.LanguageChanged -= OnLanguageChanged;
        _trackingService.EntryAdded -= RecordStreakActivity;
        _foodSearchService.FoodLogAdded -= RecordStreakActivity;
        _rewardedAdService.AdUnavailable -= OnAdUnavailable;
        _adService.AdsStateChanged -= OnAdsStateChanged;
        ProgressViewModel.FloatingTextRequested -= OnProgressFloatingText;
        ProgressViewModel.CelebrationRequested -= OnProgressCelebration;
        FoodSearchViewModel.NavigationRequested -= OnFoodSearchNavigation;

        ProgressViewModel.Dispose();
        FoodSearchViewModel.Dispose();
        SettingsViewModel.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
