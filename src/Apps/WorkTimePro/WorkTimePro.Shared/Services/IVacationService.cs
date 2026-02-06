using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for vacation management (Premium feature)
/// </summary>
public interface IVacationService
{
    /// <summary>
    /// Get vacation quota for a year
    /// </summary>
    Task<VacationQuota> GetQuotaAsync(int year, int? employerId = null);

    /// <summary>
    /// Save/update vacation quota
    /// </summary>
    Task SaveQuotaAsync(VacationQuota quota);

    /// <summary>
    /// Get all vacation entries for a year
    /// </summary>
    Task<List<VacationEntry>> GetVacationEntriesAsync(int year);

    /// <summary>
    /// Get vacation entries for a period
    /// </summary>
    Task<List<VacationEntry>> GetVacationEntriesAsync(DateTime start, DateTime end);

    /// <summary>
    /// Save vacation entry
    /// </summary>
    Task SaveVacationEntryAsync(VacationEntry entry);

    /// <summary>
    /// Delete vacation entry
    /// </summary>
    Task DeleteVacationEntryAsync(int entryId);

    /// <summary>
    /// Check if a date is a vacation day
    /// </summary>
    Task<VacationEntry?> GetVacationForDateAsync(DateTime date);

    /// <summary>
    /// Calculate work days in a vacation period
    /// </summary>
    Task<int> CalculateWorkDaysAsync(DateTime start, DateTime end);

    /// <summary>
    /// Get vacation statistics for a year
    /// </summary>
    Task<VacationStatistics> GetStatisticsAsync(int year, int? employerId = null);

    /// <summary>
    /// Carry over remaining days to next year
    /// </summary>
    Task<int> CarryOverRemainingDaysAsync(int fromYear, int toYear, int? employerId = null);
}

/// <summary>
/// Vacation statistics
/// </summary>
public class VacationStatistics
{
    public int Year { get; set; }
    public int TotalDays { get; set; }
    public int CarryOverDays { get; set; }
    public int AvailableDays => TotalDays + CarryOverDays;
    public int TakenDays { get; set; }
    public int PlannedDays { get; set; }
    public int RemainingDays => AvailableDays - TakenDays - PlannedDays;
    public int SickDays { get; set; }
    public int SpecialLeaveDays { get; set; }
    public double UsedPercent => AvailableDays > 0 ? (TakenDays * 100.0 / AvailableDays) : 0;
}
