using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Services;

namespace FitnessRechner.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IPurchaseService _purchaseService;

    /// <summary>
    /// Raised when the VM wants to navigate (e.g. go back)
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Raised when the VM wants to show a message (title, message).
    /// </summary>
    public event Action<string, string>? MessageRequested;

    /// <summary>
    /// Raised when language changes (for views to refresh UI)
    /// </summary>
    public event Action? LanguageChanged;

    /// <summary>
    /// Raised when feedback email should be opened
    /// </summary>
    public event Action<string>? FeedbackRequested;

    public SettingsViewModel(
        IThemeService themeService,
        ILocalizationService localizationService,
        IPurchaseService purchaseService)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        _purchaseService = purchaseService;

        _selectedTheme = _themeService.CurrentTheme;
        _selectedLanguage = _localizationService.CurrentLanguage;
        _isPremium = _purchaseService.IsPremium;

        _purchaseService.PremiumStatusChanged += OnPremiumStatusChanged;
    }

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private bool _isPremium;

    public bool IsNotPremium => !IsPremium;

    partial void OnIsPremiumChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotPremium));
    }

    public string AppVersion => $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.0.0"}";

    #region Language Selection

    public IReadOnlyList<LanguageInfo> AvailableLanguages => _localizationService.AvailableLanguages;

    public bool IsEnglishSelected => SelectedLanguage == "en";
    public bool IsGermanSelected => SelectedLanguage == "de";
    public bool IsSpanishSelected => SelectedLanguage == "es";
    public bool IsFrenchSelected => SelectedLanguage == "fr";
    public bool IsItalianSelected => SelectedLanguage == "it";
    public bool IsPortugueseSelected => SelectedLanguage == "pt";

    [RelayCommand]
    private void SelectLanguage(string languageCode)
    {
        if (SelectedLanguage == languageCode) return;

        SelectedLanguage = languageCode;
        _localizationService.SetLanguage(languageCode);

        UpdateLanguageProperties();

        // Notify listeners so views can refresh
        LanguageChanged?.Invoke();
    }

    private void UpdateLanguageProperties()
    {
        OnPropertyChanged(nameof(IsEnglishSelected));
        OnPropertyChanged(nameof(IsGermanSelected));
        OnPropertyChanged(nameof(IsSpanishSelected));
        OnPropertyChanged(nameof(IsFrenchSelected));
        OnPropertyChanged(nameof(IsItalianSelected));
        OnPropertyChanged(nameof(IsPortugueseSelected));
    }

    #endregion

    #region Theme Selection

    public bool IsMidnightSelected => SelectedTheme == AppTheme.Midnight;
    public bool IsAuroraSelected => SelectedTheme == AppTheme.Aurora;
    public bool IsDaylightSelected => SelectedTheme == AppTheme.Daylight;
    public bool IsForestSelected => SelectedTheme == AppTheme.Forest;

    [RelayCommand]
    private void SelectTheme(string themeName)
    {
        var theme = themeName switch
        {
            "Midnight" => AppTheme.Midnight,
            "Aurora" => AppTheme.Aurora,
            "Daylight" => AppTheme.Daylight,
            "Forest" => AppTheme.Forest,
            _ => AppTheme.Midnight
        };

        SelectedTheme = theme;
        _themeService.SetTheme(theme);

        OnPropertyChanged(nameof(IsMidnightSelected));
        OnPropertyChanged(nameof(IsAuroraSelected));
        OnPropertyChanged(nameof(IsDaylightSelected));
        OnPropertyChanged(nameof(IsForestSelected));
    }

    #endregion

    #region Premium / Purchases

    [RelayCommand]
    private async Task PurchasePremium()
    {
        var success = await _purchaseService.PurchaseRemoveAdsAsync();
        if (success)
        {
            IsPremium = _purchaseService.IsPremium;
            MessageRequested?.Invoke(
                _localizationService.GetString("AlertSuccess"),
                _localizationService.GetString("AlertPurchaseSuccess"));
        }
    }

    [RelayCommand]
    private async Task RestorePurchases()
    {
        var restored = await _purchaseService.RestorePurchasesAsync();
        if (restored)
        {
            IsPremium = _purchaseService.IsPremium;
            MessageRequested?.Invoke(
                _localizationService.GetString("AlertSuccess"),
                _localizationService.GetString("AlertPurchaseRestored"));
        }
        else
        {
            MessageRequested?.Invoke(
                _localizationService.GetString("AlertError"),
                _localizationService.GetString("AlertNoPurchases"));
        }
    }

    #endregion

    #region Feedback

    [RelayCommand]
    private void SendFeedback()
    {
        FeedbackRequested?.Invoke("FitnessRechner");
    }

    #endregion

    #region Navigation

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    #endregion

    #region Lifecycle

    public void Initialize()
    {
        SelectedTheme = _themeService.CurrentTheme;

        OnPropertyChanged(nameof(IsMidnightSelected));
        OnPropertyChanged(nameof(IsAuroraSelected));
        OnPropertyChanged(nameof(IsDaylightSelected));
        OnPropertyChanged(nameof(IsForestSelected));

        IsPremium = _purchaseService.IsPremium;
    }

    #endregion

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

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
