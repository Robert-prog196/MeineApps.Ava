using System.Globalization;
using Avalonia.Data.Converters;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts bool to string with TrueValue/FalseValue properties.
/// Note: Because this converter has settable properties, it cannot use the singleton Instance pattern.
/// Create instances in XAML resources instead.
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "True";
    public string FalseValue { get; set; } = "False";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
