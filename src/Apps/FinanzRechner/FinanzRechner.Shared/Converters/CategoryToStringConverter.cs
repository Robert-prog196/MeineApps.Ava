using System.Globalization;
using Avalonia.Data.Converters;
using FinanzRechner.Helpers;
using FinanzRechner.Models;
using MeineApps.Core.Ava.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts ExpenseCategory to localized names.
/// Supports null for "All Categories".
/// Uses CategoryLocalizationHelper for centralized translations.
/// </summary>
public class CategoryToStringConverter : IValueConverter
{
    public static readonly CategoryToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var localizationService = App.Services?.GetService<ILocalizationService>();

        // Handle null for "All Categories"
        if (value == null)
        {
            return localizationService?.GetString("AllCategories") ?? "All categories";
        }

        if (value is not ExpenseCategory category)
            return value?.ToString();

        return CategoryLocalizationHelper.GetLocalizedName(category, localizationService);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
