using System.Globalization;
using Avalonia.Data.Converters;

namespace FinanzRechner.Converters;

/// <summary>
/// Konvertiert bool zu double (z.B. für Opacity-Binding: true=1.0, false=0.5).
/// Parameter: "TrueValue,FalseValue" (z.B. "1,0.5")
/// </summary>
public class BoolToDoubleConverter : IValueConverter
{
    public static readonly BoolToDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return 1.0;

        var trueVal = 1.0;
        var falseVal = 0.5;

        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (parts.Length >= 2)
            {
                if (double.TryParse(parts[0], CultureInfo.InvariantCulture, out var tv)) trueVal = tv;
                if (double.TryParse(parts[1], CultureInfo.InvariantCulture, out var fv)) falseVal = fv;
            }
        }

        return boolValue ? trueVal : falseVal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
