using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FitnessRechner.Converters;

/// <summary>
/// Converts a value to boolean: true if not null, false otherwise.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
