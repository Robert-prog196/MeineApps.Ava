using System.Globalization;
using Avalonia.Data.Converters;

namespace HandwerkerImperium.Converters;

/// <summary>
/// Converts a decimal money value to a formatted display string.
/// Examples: 1234.56 -> "1.234 EUR", 1000000 -> "1,0M EUR"
/// </summary>
public class MoneyDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not decimal money)
            return "0 \u20AC";

        return money switch
        {
            >= 1_000_000_000 => $"{money / 1_000_000_000:F1}B \u20AC",
            >= 1_000_000 => $"{money / 1_000_000:F1}M \u20AC",
            >= 10_000 => $"{money / 1_000:F1}K \u20AC",
            _ => $"{money:N0} \u20AC"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
