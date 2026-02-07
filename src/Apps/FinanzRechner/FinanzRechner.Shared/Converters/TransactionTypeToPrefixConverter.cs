using System.Globalization;
using Avalonia.Data.Converters;
using FinanzRechner.Models;

namespace FinanzRechner.Converters;

/// <summary>
/// Konvertiert TransactionType zu Vorzeichen-String ("+" fuer Income, "-" fuer Expense).
/// </summary>
public class TransactionTypeToPrefixConverter : IValueConverter
{
    public static readonly TransactionTypeToPrefixConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
            return type == TransactionType.Income ? "+" : "-";

        return "-";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
