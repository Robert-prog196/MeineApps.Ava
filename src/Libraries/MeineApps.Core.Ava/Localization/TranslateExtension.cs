using Avalonia.Markup.Xaml;

namespace MeineApps.Core.Ava.Localization;

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: Text="{loc:Translate Calculate}"
/// </summary>
public class TranslateExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public TranslateExtension() { }
    public TranslateExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key)) return string.Empty;
        return LocalizationManager.GetString(Key);
    }
}

/// <summary>
/// Static accessor for localization. Must be initialized at app startup.
/// Used by TranslateExtension and ViewModels.
/// </summary>
public static class LocalizationManager
{
    private static ILocalizationService? _service;

    /// <summary>
    /// Initialize with the app's localization service
    /// </summary>
    public static void Initialize(ILocalizationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Current localization service instance
    /// </summary>
    public static ILocalizationService? Service => _service;

    /// <summary>
    /// Get a localized string by key
    /// </summary>
    public static string GetString(string key)
    {
        return _service?.GetString(key) ?? key;
    }
}
