using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for employer management (Premium feature for multiple employers)
/// </summary>
public interface IEmployerService
{
    /// <summary>
    /// Get all employers
    /// </summary>
    Task<List<Employer>> GetEmployersAsync(bool includeInactive = false);

    /// <summary>
    /// Get default employer
    /// </summary>
    Task<Employer?> GetDefaultEmployerAsync();

    /// <summary>
    /// Save employer
    /// </summary>
    Task SaveEmployerAsync(Employer employer);

    /// <summary>
    /// Delete employer
    /// </summary>
    Task DeleteEmployerAsync(int id);

    /// <summary>
    /// Set employer as default
    /// </summary>
    Task SetDefaultEmployerAsync(int id);

    /// <summary>
    /// Get work hours per employer for a period
    /// </summary>
    Task<Dictionary<Employer, double>> GetEmployerHoursAsync(DateTime start, DateTime end);

    /// <summary>
    /// Get statistics for an employer
    /// </summary>
    Task<EmployerStatistics> GetEmployerStatisticsAsync(int employerId);
}

/// <summary>
/// Employer statistics
/// </summary>
public class EmployerStatistics
{
    public int EmployerId { get; set; }
    public string EmployerName { get; set; } = "";
    public double TotalHours { get; set; }
    public double ThisMonthHours { get; set; }
    public double TargetHoursWeekly { get; set; }
    public int WorkDaysCount { get; set; }
    public double AverageHoursPerDay { get; set; }
}
