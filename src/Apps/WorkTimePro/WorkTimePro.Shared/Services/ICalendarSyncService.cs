using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service Interface for Google Calendar synchronisation (Premium feature)
/// </summary>
public interface ICalendarSyncService
{
    /// <summary>
    /// Is the user connected to Google Calendar?
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Email of the connected Google account
    /// </summary>
    string? ConnectedEmail { get; }

    /// <summary>
    /// Name of the selected calendar
    /// </summary>
    string? CalendarName { get; }

    /// <summary>
    /// Date of the last sync
    /// </summary>
    DateTime? LastSyncDate { get; }

    /// <summary>
    /// Event when connection status changes
    /// </summary>
    event EventHandler<bool>? ConnectionChanged;

    // === Connection ===

    /// <summary>
    /// Connect to Google Calendar
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Disconnect
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Get available calendars
    /// </summary>
    Task<List<CalendarInfo>> GetAvailableCalendarsAsync();

    /// <summary>
    /// Select target calendar
    /// </summary>
    Task SetTargetCalendarAsync(string calendarId);

    // === Synchronisation ===

    /// <summary>
    /// Sync work days with calendar
    /// </summary>
    Task<CalendarSyncResult> SyncWorkDaysAsync(DateTime start, DateTime end);

    /// <summary>
    /// Export single work day
    /// </summary>
    Task<bool> ExportWorkDayAsync(WorkDay workDay);

    /// <summary>
    /// Export vacation
    /// </summary>
    Task<bool> ExportVacationAsync(VacationEntry vacation);

    /// <summary>
    /// Delete event
    /// </summary>
    Task<bool> DeleteEventAsync(string eventId);

    // === Settings ===

    /// <summary>
    /// Save sync settings
    /// </summary>
    Task SetSyncOptionsAsync(CalendarSyncOptions options);

    /// <summary>
    /// Get current sync settings
    /// </summary>
    CalendarSyncOptions GetSyncOptions();
}

/// <summary>
/// Information about a calendar
/// </summary>
public class CalendarInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackgroundColor { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// Result of a calendar sync
/// </summary>
public class CalendarSyncResult
{
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public int CreatedEvents { get; set; }
    public int UpdatedEvents { get; set; }
    public int DeletedEvents { get; set; }
    public int SkippedEvents { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Options for calendar sync
/// </summary>
public class CalendarSyncOptions
{
    /// <summary>
    /// Sync work days
    /// </summary>
    public bool SyncWorkDays { get; set; } = true;

    /// <summary>
    /// Sync vacation
    /// </summary>
    public bool SyncVacation { get; set; } = true;

    /// <summary>
    /// Sync holidays
    /// </summary>
    public bool SyncHolidays { get; set; } = false;

    /// <summary>
    /// Sync projects as separate events
    /// </summary>
    public bool SyncProjects { get; set; } = false;

    /// <summary>
    /// Only days with entries
    /// </summary>
    public bool OnlyDaysWithEntries { get; set; } = true;

    /// <summary>
    /// Show overtime in title
    /// </summary>
    public bool ShowOvertimeInTitle { get; set; } = true;

    /// <summary>
    /// Color for work days
    /// </summary>
    public string WorkDayColor { get; set; } = "#1565C0";

    /// <summary>
    /// Color for vacation
    /// </summary>
    public string VacationColor { get; set; } = "#2196F3";

    /// <summary>
    /// Color for holidays
    /// </summary>
    public string HolidayColor { get; set; } = "#FF9800";
}
