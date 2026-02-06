using System.Globalization;
using Avalonia.Data.Converters;

namespace HandwerkerRechner.Converters;

/// <summary>
/// Converts a value to bool (true if not null)
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
