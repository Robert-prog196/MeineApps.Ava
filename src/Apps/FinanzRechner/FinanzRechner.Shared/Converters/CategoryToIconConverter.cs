using System.Globalization;
using Avalonia.Data.Converters;
using FinanzRechner.Models;
using Material.Icons;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts ExpenseCategory to Material.Icons.MaterialIconKind enum values.
/// Use with Material.Icons.Avalonia MaterialIcon control.
/// </summary>
public class CategoryToIconConverter : IValueConverter
{
    public static readonly CategoryToIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExpenseCategory category)
        {
            return category switch
            {
                // Expenses
                ExpenseCategory.Food => MaterialIconKind.FoodApple,
                ExpenseCategory.Transport => MaterialIconKind.Car,
                ExpenseCategory.Housing => MaterialIconKind.Home,
                ExpenseCategory.Entertainment => MaterialIconKind.MovieOpen,
                ExpenseCategory.Shopping => MaterialIconKind.Cart,
                ExpenseCategory.Health => MaterialIconKind.Pill,
                ExpenseCategory.Education => MaterialIconKind.BookOpenPageVariant,
                ExpenseCategory.Bills => MaterialIconKind.FileDocument,
                ExpenseCategory.Other => MaterialIconKind.PackageVariant,
                // Income
                ExpenseCategory.Salary => MaterialIconKind.CashMultiple,
                ExpenseCategory.Freelance => MaterialIconKind.Briefcase,
                ExpenseCategory.Investment => MaterialIconKind.TrendingUp,
                ExpenseCategory.Gift => MaterialIconKind.Gift,
                ExpenseCategory.OtherIncome => MaterialIconKind.Cash,
                _ => MaterialIconKind.PackageVariant
            };
        }
        return MaterialIconKind.PackageVariant;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
