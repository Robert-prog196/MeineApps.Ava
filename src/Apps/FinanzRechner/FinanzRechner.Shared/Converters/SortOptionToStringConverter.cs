using System.Globalization;
using Avalonia.Data.Converters;
using FinanzRechner.ViewModels;
using MeineApps.Core.Ava.Localization;
using Microsoft.Extensions.DependencyInjection;
using static FinanzRechner.ViewModels.ExpenseTrackerViewModel;

namespace FinanzRechner.Converters;

/// <summary>
/// Converts SortOption to localized string.
/// </summary>
public class SortOptionToStringConverter : IValueConverter
{
    public static readonly SortOptionToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SortOption sortOption)
            return value?.ToString();

        var localizationService = App.Services?.GetService<ILocalizationService>();

        if (localizationService == null)
            return sortOption.ToString();

        return sortOption switch
        {
            SortOption.DateDescending =>
                localizationService.GetString("SortDateDescending") ?? "Newest first",
            SortOption.DateAscending =>
                localizationService.GetString("SortDateAscending") ?? "Oldest first",
            SortOption.AmountDescending =>
                localizationService.GetString("SortAmountDescending") ?? "Highest amount",
            SortOption.AmountAscending =>
                localizationService.GetString("SortAmountAscending") ?? "Lowest amount",
            SortOption.Description =>
                localizationService.GetString("SortDescription") ?? "A-Z",
            _ => sortOption.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
