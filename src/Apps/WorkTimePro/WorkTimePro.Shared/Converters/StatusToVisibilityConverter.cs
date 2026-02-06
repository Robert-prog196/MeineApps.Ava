using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WorkTimePro.Models;

namespace WorkTimePro.Converters;

/// <summary>
/// Konvertiert TrackingStatus zu Visibility (für Pause-Button)
/// </summary>
public class StatusToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TrackingStatus status)
        {
            // Pause-Button nur sichtbar wenn Working oder OnBreak
            return status != TrackingStatus.Idle;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert Prozent (0-100) zu Progress (0-1)
/// </summary>
public class PercentToProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return percent / 100.0;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert TimeSpan zu String (HH:mm Format)
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            var totalHours = (int)Math.Abs(ts.TotalHours);
            var minutes = Math.Abs(ts.Minutes);
            var sign = ts.TotalMinutes < 0 ? "-" : "";
            return $"{sign}{totalHours}:{minutes:D2}";
        }
        return "0:00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert Balance-Minuten zu Farbe (grün/rot)
/// </summary>
public class BalanceToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            return minutes >= 0
                ? SolidColorBrush.Parse("#4CAF50")
                : SolidColorBrush.Parse("#F44336");
        }
        return SolidColorBrush.Parse("#9E9E9E");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert DayStatus zu Farbe
/// </summary>
public class DayStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DayStatus status)
        {
            return status switch
            {
                DayStatus.WorkDay => SolidColorBrush.Parse("#4CAF50"),
                DayStatus.Weekend => SolidColorBrush.Parse("#9E9E9E"),
                DayStatus.Vacation => SolidColorBrush.Parse("#2196F3"),
                DayStatus.Holiday => SolidColorBrush.Parse("#FF9800"),
                DayStatus.Sick => SolidColorBrush.Parse("#F44336"),
                DayStatus.HomeOffice => SolidColorBrush.Parse("#9C27B0"),
                DayStatus.BusinessTrip => SolidColorBrush.Parse("#00BCD4"),
                _ => SolidColorBrush.Parse("#757575")
            };
        }
        return SolidColorBrush.Parse("#757575");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert Bool zu Auto-Pause Icon (Lightning oder leer)
/// </summary>
public class AutoPauseToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAutoPause && isAutoPause)
        {
            return WorkTimePro.Helpers.Icons.Lightning;
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Konvertiert Arbeitsminuten zu Heatmap-Farbe
/// </summary>
public class HeatmapValueToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            // 0 Minuten = keine Farbe
            if (minutes == 0)
                return SolidColorBrush.Parse("#EEEEEE");

            // Bis 4h = hellgrün
            if (minutes < 240)
                return SolidColorBrush.Parse("#C8E6C9");

            // 4-6h = mittelgrün
            if (minutes < 360)
                return SolidColorBrush.Parse("#81C784");

            // 6-8h = dunkelgrün
            if (minutes < 480)
                return SolidColorBrush.Parse("#4CAF50");

            // 8-10h = normal
            if (minutes < 600)
                return SolidColorBrush.Parse("#388E3C");

            // Über 10h = rot (Überstunden)
            return SolidColorBrush.Parse("#F44336");
        }
        return SolidColorBrush.Parse("#EEEEEE");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
