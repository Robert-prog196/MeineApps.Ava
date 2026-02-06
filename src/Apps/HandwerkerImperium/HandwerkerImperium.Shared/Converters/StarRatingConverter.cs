using System.Globalization;
using Avalonia.Data.Converters;
using HandwerkerImperium.Helpers;

namespace HandwerkerImperium.Converters;

/// <summary>
/// Converts star rating (1-3) to MDI star glyph string.
/// Use with FontFamily="MDI" on the TextBlock.
/// </summary>
public class StarRatingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int stars)
            return "";

        return stars switch
        {
            1 => Icons.Star,
            2 => Icons.Star + Icons.Star,
            3 => Icons.Star + Icons.Star + Icons.Star,
            _ => ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
