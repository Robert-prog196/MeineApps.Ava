using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for holiday management (Premium feature)
/// </summary>
public interface IHolidayService
{
    /// <summary>
    /// Load holidays for a year (based on settings)
    /// </summary>
    Task<List<HolidayEntry>> GetHolidaysAsync(int year);

    /// <summary>
    /// Load holidays for a date range
    /// </summary>
    Task<List<HolidayEntry>> GetHolidaysAsync(DateTime start, DateTime end);

    /// <summary>
    /// Check if a date is a holiday
    /// </summary>
    Task<HolidayEntry?> GetHolidayForDateAsync(DateTime date);

    /// <summary>
    /// Calculate all holidays for a year in a region
    /// </summary>
    List<HolidayEntry> CalculateHolidays(int year, string region);

    /// <summary>
    /// Get available regions (German states)
    /// </summary>
    List<(string Code, string Name)> GetAvailableRegions();
}
