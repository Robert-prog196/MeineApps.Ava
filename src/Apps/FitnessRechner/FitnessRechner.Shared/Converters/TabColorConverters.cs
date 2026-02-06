using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FitnessRechner.Converters;

/// <summary>
/// Provides static converter instances for tab background and text colors.
/// Each converter maps a bool (isActive) to an IBrush.
/// Active tabs get a semi-transparent accent color; inactive tabs are transparent.
/// Text converters return the appropriate foreground brush.
/// </summary>
public static class TabColorConverters
{
    /// <summary>Weight tab: SuccessBrush at 20% opacity when active</summary>
    public static readonly IValueConverter WeightTab = new TabBackgroundConverter("SuccessBrush");

    /// <summary>Body tab: InfoBrush at 20% opacity when active</summary>
    public static readonly IValueConverter BodyTab = new TabBackgroundConverter("InfoBrush");

    /// <summary>Body Fat sub-tab: WarningBrush at 20% opacity when active</summary>
    public static readonly IValueConverter BodyFatTab = new TabBackgroundConverter("WarningBrush");

    /// <summary>Water tab: InfoBrush at 20% opacity when active</summary>
    public static readonly IValueConverter WaterTab = new TabBackgroundConverter("InfoBrush");

    /// <summary>Calories tab: ErrorBrush at 20% opacity when active</summary>
    public static readonly IValueConverter CaloriesTab = new TabBackgroundConverter("ErrorBrush");

    /// <summary>Returns PrimaryBrush when active, TextMutedBrush when inactive</summary>
    public static readonly IValueConverter ActiveText = new TabTextConverter();
}

/// <summary>
/// Converts bool (isActive) to a tab background brush.
/// Active: theme brush at 20% opacity. Inactive: transparent.
/// </summary>
internal class TabBackgroundConverter : IValueConverter
{
    private readonly string _brushKey;

    public TabBackgroundConverter(string brushKey)
    {
        _brushKey = brushKey;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            // Try to resolve the brush from the current theme
            if (Application.Current?.TryGetResource(_brushKey, Application.Current.ActualThemeVariant, out var resource) == true
                && resource is ISolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return new SolidColorBrush(new Color(50, color.R, color.G, color.B)); // ~20% opacity
            }

            // Fallback: semi-transparent primary
            return new SolidColorBrush(new Color(50, 100, 100, 255));
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts bool (isActive) to a text foreground brush.
/// Active: TextPrimaryBrush (white/dark depending on theme). Inactive: TextMutedBrush.
/// </summary>
internal class TabTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is bool isActive && isActive ? "TextPrimaryBrush" : "TextMutedBrush";

        if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        // Fallback
        return value is bool b && b ? Brushes.White : Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
