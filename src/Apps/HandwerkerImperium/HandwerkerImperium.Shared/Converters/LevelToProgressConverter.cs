using System.Globalization;
using Avalonia.Data.Converters;

namespace HandwerkerImperium.Converters;

/// <summary>
/// Converts workshop level to progress value (0.0 - 1.0) based on max level.
/// </summary>
public class LevelToProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double progress = 0.0;

        // Support both int (level) and double (position) values
        if (value is int intValue)
        {
            progress = intValue;
        }
        else if (value is double doubleValue)
        {
            progress = doubleValue;
        }
        else
        {
            return 0.0;
        }

        // Parse multiplier from parameter (e.g., "300" for pixel width)
        double multiplier = 10.0; // Default max level
        if (parameter is string paramStr && double.TryParse(paramStr, out double parsedMultiplier))
        {
            multiplier = parsedMultiplier;
        }

        // For pixel-based positioning (large multiplier like 300), multiply directly
        // For progress bars (small multiplier like 10), calculate ratio
        if (multiplier > 100)
        {
            // Pixel positioning: progress (0-1) * width
            return progress * multiplier;
        }
        else
        {
            // Progress ratio: level / maxLevel
            return Math.Min(1.0, progress / multiplier);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
