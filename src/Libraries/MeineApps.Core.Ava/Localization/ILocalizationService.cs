using System.Globalization;

namespace MeineApps.Core.Ava.Localization;

/// <summary>
/// Information about an available language
/// </summary>
public record LanguageInfo(string Code, string NativeName, string EnglishName);

/// <summary>
/// Service for multi-language support
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Current language code (e.g. "de", "en")
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Current CultureInfo
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Available languages
    /// </summary>
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>
    /// Set the current language
    /// </summary>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Get a localized string by key
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Initialize the service (load saved or device language)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Event raised when language changes
    /// </summary>
    event EventHandler? LanguageChanged;
}
