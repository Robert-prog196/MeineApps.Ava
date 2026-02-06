using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts a bool to a Brush from application resources.
/// TrueColorKey = resource key when true, FalseColorKey = resource key when false.
/// Note: Because this converter has settable properties, it cannot use the singleton Instance pattern.
/// Create instances in XAML resources instead.
/// </summary>
public class BoolToResourceColorConverter : IValueConverter
{
    public string TrueColorKey { get; set; } = "PrimaryColor";
    public string FalseColorKey { get; set; } = "ButtonBackgroundColor";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        var resourceKey = isTrue ? TrueColorKey : FalseColorKey;

        var app = Application.Current;
        if (app != null && app.TryGetResource(resourceKey, app.ActualThemeVariant, out var resourceValue) && resourceValue is IBrush brush)
        {
            return brush;
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
