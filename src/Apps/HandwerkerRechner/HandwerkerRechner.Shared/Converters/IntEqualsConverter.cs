using System.Globalization;
using Avalonia.Data.Converters;

namespace HandwerkerRechner.Converters;

/// <summary>
/// Converts an int value to bool by comparing with ConverterParameter.
/// Usage: Converter={StaticResource IntEquals}, ConverterParameter=0
/// </summary>
public class IntEqualsConverter : IValueConverter
{
    public static readonly IntEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramStr && int.TryParse(paramStr, out var paramInt))
            return intValue == paramInt;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
