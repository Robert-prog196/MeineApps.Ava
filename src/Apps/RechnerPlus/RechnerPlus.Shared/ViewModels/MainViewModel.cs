using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;

namespace RechnerPlus.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
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
        CalculatorViewModel.FloatingTextRequested += OnCalculatorFloatingText;
    }

    private void OnCalculatorFloatingText(string text, string category)
    {
        FloatingTextRequested?.Invoke(text, category);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavCalculatorText));
        OnPropertyChanged(nameof(NavConverterText));
        OnPropertyChanged(nameof(NavSettingsText));
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        // Beim Wechsel zum Rechner: Zahlenformat aktualisieren (falls in Settings geändert)
        if (value == 0)
            CalculatorViewModel.RefreshNumberFormat();
    }

    /// <summary>
    /// Behandelt die Zurück-Taste. Gibt true zurück wenn intern navigiert wurde,
    /// false wenn die App geschlossen werden soll.
    /// Reihenfolge: History schließen → Bestätigungsdialog schließen → Tab zum Rechner zurück.
    /// </summary>
    public bool HandleBack()
    {
        // 1. History-Panel offen → schließen
        if (CalculatorViewModel.IsHistoryVisible)
        {
            CalculatorViewModel.HideHistoryCommand.Execute(null);
            return true;
        }

        // 2. Bestätigungsdialog offen → schließen
        if (CalculatorViewModel.ShowClearHistoryConfirm)
        {
            CalculatorViewModel.CancelClearHistoryCommand.Execute(null);
            return true;
        }

        // 3. Nicht auf dem Rechner-Tab → zurück zum Rechner
        if (SelectedTabIndex != 0)
        {
            SelectedTabIndex = 0;
            return true;
        }

        // 4. Bereits auf dem Rechner-Tab, nichts offen → App soll schließen
        return false;
    }

    [RelayCommand]
    private void NavigateToCalculator() => SelectedTabIndex = 0;

    [RelayCommand]
    private void NavigateToConverter() => SelectedTabIndex = 1;

    [RelayCommand]
    private void NavigateToSettings() => SelectedTabIndex = 2;

    public void Dispose()
    {
        if (_disposed) return;
        _localization.LanguageChanged -= OnLanguageChanged;
        CalculatorViewModel.FloatingTextRequested -= OnCalculatorFloatingText;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
