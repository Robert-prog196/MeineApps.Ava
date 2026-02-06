using System.Globalization;
using Avalonia.Data.Converters;
using FinanzRechner.ViewModels;
using MeineApps.Core.Ava.Localization;
using Microsoft.Extensions.DependencyInjection;
using static FinanzRechner.ViewModels.ExpenseTrackerViewModel;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts FilterTypeOption to localized string.
/// </summary>
public class FilterTypeToStringConverter : IValueConverter
{
    public static readonly FilterTypeToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FilterTypeOption filterType)
            return value?.ToString();

        var localizationService = App.Services?.GetService<ILocalizationService>();

        if (localizationService == null)
            return filterType.ToString();

        return filterType switch
        {
            FilterTypeOption.All =>
                localizationService.GetString("FilterAll") ?? "All",
            FilterTypeOption.Expenses =>
                localizationService.GetString("FilterExpenses") ?? "Expenses only",
            FilterTypeOption.Income =>
                localizationService.GetString("FilterIncome") ?? "Income only",
            _ => filterType.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
