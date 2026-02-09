using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;

namespace RechnerPlus.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCalculatorActive))]
    [NotifyPropertyChangedFor(nameof(IsConverterActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private int _selectedTabIndex;

    [ObservableProperty]
    private CalculatorViewModel _calculatorViewModel;

    [ObservableProperty]
    private ConverterViewModel _converterViewModel;

    [ObservableProperty]
    private SettingsViewModel _settingsViewModel;

    // Localized tab labels
    public string NavCalculatorText => _localization.GetString("NavCalculator");
    public string NavConverterText => _localization.GetString("NavConverter");
    public string NavSettingsText => _localization.GetString("NavSettings");

    // Active tab indicators
    public bool IsCalculatorActive => SelectedTabIndex == 0;
    public bool IsConverterActive => SelectedTabIndex == 1;
    public bool IsSettingsActive => SelectedTabIndex == 2;

    /// <summary>Event fuer Floating-Text-Anzeige (Text, Kategorie).</summary>
    public event Action<string, string>? FloatingTextRequested;

    public MainViewModel(
        IThemeService themeService,
        ILocalizationService localization,
        CalculatorViewModel calculatorViewModel,
        ConverterViewModel converterViewModel,
        SettingsViewModel settingsViewModel)
    {
        _themeService = themeService;
        _localization = localization;
        _calculatorViewModel = calculatorViewModel;
        _converterViewModel = converterViewModel;
        _settingsViewModel = settingsViewModel;

        _localization.LanguageChanged += OnLanguageChanged;

        // Floating-Text-Events vom Calculator weiterleiten
        _calculatorViewModel.FloatingTextRequested += (text, cat) => FloatingTextRequested?.Invoke(text, cat);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavCalculatorText));
        OnPropertyChanged(nameof(NavConverterText));
        OnPropertyChanged(nameof(NavSettingsText));
    }

    [RelayCommand]
    private void NavigateToCalculator() => SelectedTabIndex = 0;

    [RelayCommand]
    private void NavigateToConverter() => SelectedTabIndex = 1;

    [RelayCommand]
    private void NavigateToSettings() => SelectedTabIndex = 2;
}
