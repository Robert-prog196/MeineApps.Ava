using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;

namespace MeineApps.Core.Ava.Services;

/// <summary>
/// Service for managing application themes in Avalonia.
/// Themes are applied dynamically - do NOT include theme .axaml files in App.axaml.
/// Only ThemeColors.axaml (shared design tokens) should be included statically.
/// </summary>
public class ThemeService : IThemeService
{
    private const string PreferenceKey = "app_theme";
    private readonly IPreferencesService _preferences;
    private AppTheme _currentTheme = AppTheme.Midnight;

    private readonly Dictionary<AppTheme, StyleInclude> _themeStyles = new();
    private StyleInclude? _currentThemeStyle;

    public ThemeService(IPreferencesService preferences)
    {
        _preferences = preferences;
        LoadSavedTheme();
    }

    public AppTheme CurrentTheme => _currentTheme;

    public bool IsDarkTheme => _currentTheme != AppTheme.Daylight;

    public IReadOnlyList<AppTheme> AvailableThemes { get; } =
        [AppTheme.Midnight, AppTheme.Aurora, AppTheme.Daylight, AppTheme.Forest];

    public event EventHandler<AppTheme>? ThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme == theme) return;

        _currentTheme = theme;
        ApplyTheme(theme);
        SaveTheme(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    /// <summary>
    /// Lädt ein Theme-StyleInclude on-demand (Lazy-Loading).
    /// Nur das aktive Theme wird geladen, nicht alle 4 beim Start.
    /// </summary>
    private StyleInclude GetOrLoadThemeStyle(AppTheme theme)
    {
        if (!_themeStyles.TryGetValue(theme, out var style))
        {
            var themeUri = new Uri($"avares://MeineApps.Core.Ava/Themes/{theme}Theme.axaml");
            style = new StyleInclude(themeUri) { Source = themeUri };
            _themeStyles[theme] = style;
        }
        return style;
    }

    private void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        void DoApply()
        {
            // Altes Theme entfernen
            if (_currentThemeStyle != null)
            {
                app.Styles.Remove(_currentThemeStyle);
            }

            // Neues Theme lazy laden und anwenden
            var style = GetOrLoadThemeStyle(theme);
            _currentThemeStyle = style;
            app.Styles.Add(style);

            // Light/Dark Toggle für FluentTheme
            app.RequestedThemeVariant = IsDarkTheme
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        // Synchron auf UI-Thread, sonst posten
        if (Dispatcher.UIThread.CheckAccess())
        {
            DoApply();
        }
        else
        {
            Dispatcher.UIThread.Post(DoApply);
        }
    }

    private void LoadSavedTheme()
    {
        var savedTheme = _preferences.Get(PreferenceKey, nameof(AppTheme.Midnight));
        if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
        {
            _currentTheme = theme;
            ApplyTheme(theme);
        }
        else
        {
            // Default: Midnight
            ApplyTheme(AppTheme.Midnight);
        }
    }

    private void SaveTheme(AppTheme theme)
    {
        _preferences.Set(PreferenceKey, theme.ToString());
    }
}
