using System.Globalization;
using Avalonia.Data.Converters;

namespace HandwerkerImperium.Converters;

/// <summary>
/// Converts a boolean to an opacity value.
/// True = 0.4 (dimmed), False = 1.0 (full opacity)
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If connected (true), dim the wire
            return boolValue ? 0.4 : 1.0;
        }

        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
