namespace MeineApps.Core.Ava.Services;

/// <summary>
/// Available app themes
/// </summary>
public enum AppTheme
{
    Midnight,   // Dark, Primary (Indigo)
    Aurora,     // Dark, Colorful (Pink/Violet/Cyan)
    Daylight,   // Light, Clean (Blue)
    Forest      // Dark, Natural (Green)
}

/// <summary>
/// Service for managing application themes
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Current active theme
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Whether the current theme is dark
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Available themes
    /// </summary>
    IReadOnlyList<AppTheme> AvailableThemes { get; }

    /// <summary>
    /// Set the active theme
    /// </summary>
    void SetTheme(AppTheme theme);

    /// <summary>
    /// Event raised when theme changes
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;
}
