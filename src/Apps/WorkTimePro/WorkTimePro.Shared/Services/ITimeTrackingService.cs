using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for time tracking (check-in/out, pauses)
/// </summary>
public interface ITimeTrackingService
{
    /// <summary>
    /// Current tracking status
    /// </summary>
    TrackingStatus CurrentStatus { get; }

    /// <summary>
    /// Event when status changes
    /// </summary>
    event EventHandler<TrackingStatus>? StatusChanged;

    /// <summary>
    /// Check in
    /// </summary>
    Task<TimeEntry> CheckInAsync(int? employerId = null, int? projectId = null, string? note = null);

    /// <summary>
    /// Check out
    /// </summary>
    Task<TimeEntry> CheckOutAsync(string? note = null);

    /// <summary>
    /// Start pause
    /// </summary>
    Task<PauseEntry> StartPauseAsync(string? note = null);

    /// <summary>
    /// End pause
    /// </summary>
    Task<PauseEntry> EndPauseAsync();

    /// <summary>
    /// Load current status
    /// </summary>
    Task LoadStatusAsync();

    /// <summary>
    /// Get today's work day
    /// </summary>
    Task<WorkDay> GetTodayAsync();

    /// <summary>
    /// Get current work time (today, running)
    /// </summary>
    Task<TimeSpan> GetCurrentWorkTimeAsync();

    /// <summary>
    /// Get current pause time (today)
    /// </summary>
    Task<TimeSpan> GetCurrentPauseTimeAsync();

    /// <summary>
    /// Time until end of work (at 8h target)
    /// </summary>
    Task<TimeSpan?> GetTimeUntilEndAsync();

    /// <summary>
    /// Add manual time entry
    /// </summary>
    Task AddManualEntryAsync(DateTime timestamp, EntryType type, string? note = null);

    /// <summary>
    /// Update time entry
    /// </summary>
    Task UpdateTimeEntryAsync(int entryId, DateTime newTimestamp, string? note = null);

    /// <summary>
    /// Update pause entry
    /// </summary>
    Task UpdatePauseEntryAsync(int pauseId, DateTime newStart, DateTime newEnd);

    /// <summary>
    /// Get current status async (for widget)
    /// </summary>
    Task<TrackingStatus> GetCurrentStatusAsync();

    /// <summary>
    /// Duration of current session (since check-in)
    /// </summary>
    TimeSpan GetCurrentSessionDuration();
}
