using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FinanzRechner.Models;

namespace FinanzRechner.Converters;

/// <summary>
/// Converter that returns Bold FontWeight when TransactionType matches TargetType, Normal otherwise.
/// Note: Because this converter has a settable property, it cannot use the singleton Instance pattern.
/// Create instances in XAML resources instead.
/// </summary>
public class TransactionTypeFontAttributesConverter : IValueConverter
{
    public TransactionType TargetType { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType currentType)
        {
            return currentType == TargetType ? FontWeight.Bold : FontWeight.Normal;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
