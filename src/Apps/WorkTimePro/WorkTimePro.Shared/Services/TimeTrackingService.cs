using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for time tracking (check-in/out, pauses)
/// </summary>
public class TimeTrackingService : ITimeTrackingService
{
    private readonly IDatabaseService _database;
    private readonly ICalculationService _calculation;

    // Cache for non-blocking GetCurrentSessionDuration()
    private TimeSpan _cachedWorkTime = TimeSpan.Zero;

    public TrackingStatus CurrentStatus { get; private set; } = TrackingStatus.Idle;

    public event EventHandler<TrackingStatus>? StatusChanged;

    public TimeTrackingService(IDatabaseService database, ICalculationService calculation)
    {
        _database = database;
        _calculation = calculation;
    }

    public async Task LoadStatusAsync()
    {
        var today = await GetTodayAsync();
        var lastEntry = await _database.GetLastTimeEntryAsync(today.Id);
        var activePause = await _database.GetActivePauseAsync(today.Id);

        if (activePause != null)
        {
            CurrentStatus = TrackingStatus.OnBreak;
        }
        else if (lastEntry?.Type == EntryType.CheckIn)
        {
            CurrentStatus = TrackingStatus.Working;
        }
        else
        {
            CurrentStatus = TrackingStatus.Idle;
        }

        StatusChanged?.Invoke(this, CurrentStatus);
    }

    public async Task<TimeEntry> CheckInAsync(int? employerId = null, int? projectId = null, string? note = null)
    {
        var today = await _database.GetOrCreateWorkDayAsync(DateTime.Today);
        var now = DateTime.Now;

        var entry = new TimeEntry
        {
            WorkDayId = today.Id,
            EmployerId = employerId,
            ProjectId = projectId,
            Timestamp = now,
            Type = EntryType.CheckIn,
            Note = note
        };

        await _database.SaveTimeEntryAsync(entry);

        // First check-in of the day?
        if (today.FirstCheckIn == null)
        {
            today.FirstCheckIn = now;
            await _database.SaveWorkDayAsync(today);
        }

        CurrentStatus = TrackingStatus.Working;
        StatusChanged?.Invoke(this, CurrentStatus);

        return entry;
    }

    public async Task<TimeEntry> CheckOutAsync(string? note = null)
    {
        var today = await GetTodayAsync();
        var now = DateTime.Now;

        // End active pause if any
        var activePause = await _database.GetActivePauseAsync(today.Id);
        if (activePause != null)
        {
            activePause.EndTime = now;
            await _database.SavePauseEntryAsync(activePause);
        }

        var entry = new TimeEntry
        {
            WorkDayId = today.Id,
            Timestamp = now,
            Type = EntryType.CheckOut,
            Note = note
        };

        await _database.SaveTimeEntryAsync(entry);

        // Update work day
        today.LastCheckOut = now;
        await _calculation.RecalculateWorkDayAsync(today);

        CurrentStatus = TrackingStatus.Idle;
        StatusChanged?.Invoke(this, CurrentStatus);

        return entry;
    }

    public async Task<PauseEntry> StartPauseAsync(string? note = null)
    {
        var today = await GetTodayAsync();
        var now = DateTime.Now;

        var pause = new PauseEntry
        {
            WorkDayId = today.Id,
            StartTime = now,
            Type = PauseType.Manual,
            IsAutoPause = false,
            Note = note
        };

        await _database.SavePauseEntryAsync(pause);

        CurrentStatus = TrackingStatus.OnBreak;
        StatusChanged?.Invoke(this, CurrentStatus);

        return pause;
    }

    public async Task<PauseEntry> EndPauseAsync()
    {
        var today = await GetTodayAsync();
        var activePause = await _database.GetActivePauseAsync(today.Id);

        if (activePause == null)
            throw new InvalidOperationException("No active pause");

        activePause.EndTime = DateTime.Now;
        await _database.SavePauseEntryAsync(activePause);

        // Update pause time
        await _calculation.RecalculatePauseTimeAsync(today);

        CurrentStatus = TrackingStatus.Working;
        StatusChanged?.Invoke(this, CurrentStatus);

        return activePause;
    }

    public async Task<WorkDay> GetTodayAsync()
    {
        return await _database.GetOrCreateWorkDayAsync(DateTime.Today);
    }

    public async Task<TimeSpan> GetCurrentWorkTimeAsync()
    {
        var today = await GetTodayAsync();
        var entries = await _database.GetTimeEntriesAsync(today.Id);
        var pauses = await _database.GetPauseEntriesAsync(today.Id);

        if (entries.Count == 0)
        {
            _cachedWorkTime = TimeSpan.Zero;
            return TimeSpan.Zero;
        }

        var totalWork = TimeSpan.Zero;
        TimeEntry? lastCheckIn = null;

        foreach (var entry in entries.OrderBy(e => e.Timestamp))
        {
            if (entry.Type == EntryType.CheckIn)
            {
                lastCheckIn = entry;
            }
            else if (entry.Type == EntryType.CheckOut && lastCheckIn != null)
            {
                totalWork += entry.Timestamp - lastCheckIn.Timestamp;
                lastCheckIn = null;
            }
        }

        // Still checked in?
        if (lastCheckIn != null && CurrentStatus == TrackingStatus.Working)
        {
            totalWork += DateTime.Now - lastCheckIn.Timestamp;
        }

        // Subtract pauses
        var totalPauses = pauses
            .Where(p => p.EndTime != null)
            .Sum(p => p.Duration.TotalMinutes);

        // Active pause
        var activePause = pauses.FirstOrDefault(p => p.EndTime == null);
        if (activePause != null)
        {
            totalPauses += (DateTime.Now - activePause.StartTime).TotalMinutes;
        }

        var result = totalWork - TimeSpan.FromMinutes(totalPauses);
        _cachedWorkTime = result; // Update cache for non-blocking GetCurrentSessionDuration()
        return result;
    }

    public async Task<TimeSpan> GetCurrentPauseTimeAsync()
    {
        var today = await GetTodayAsync();
        var pauses = await _database.GetPauseEntriesAsync(today.Id);

        var totalPauses = pauses
            .Where(p => !p.IsAutoPause && p.EndTime != null)
            .Sum(p => p.Duration.TotalMinutes);

        // Active pause
        var activePause = pauses.FirstOrDefault(p => p.EndTime == null);
        if (activePause != null)
        {
            totalPauses += (DateTime.Now - activePause.StartTime).TotalMinutes;
        }

        return TimeSpan.FromMinutes(totalPauses);
    }

    public async Task<TimeSpan?> GetTimeUntilEndAsync()
    {
        if (CurrentStatus == TrackingStatus.Idle)
            return null;

        var today = await GetTodayAsync();
        var currentWork = await GetCurrentWorkTimeAsync();
        var targetWork = today.TargetWorkTime;

        if (currentWork >= targetWork)
            return TimeSpan.Zero;

        return targetWork - currentWork;
    }

    public async Task AddManualEntryAsync(DateTime timestamp, EntryType type, string? note = null)
    {
        var workDay = await _database.GetOrCreateWorkDayAsync(timestamp.Date);

        var entry = new TimeEntry
        {
            WorkDayId = workDay.Id,
            Timestamp = timestamp,
            Type = type,
            Note = note,
            IsManuallyEdited = true
        };

        await _database.SaveTimeEntryAsync(entry);

        // Recalculate work day
        await _calculation.RecalculateWorkDayAsync(workDay);
    }

    public async Task UpdateTimeEntryAsync(int entryId, DateTime newTimestamp, string? note = null)
    {
        // Optimized: Direct lookup by ID
        var entry = await _database.GetTimeEntryByIdAsync(entryId);

        if (entry != null)
        {
            if (!entry.IsManuallyEdited)
            {
                entry.OriginalTimestamp = entry.Timestamp;
            }
            entry.Timestamp = newTimestamp;
            entry.IsManuallyEdited = true;
            if (note != null)
                entry.Note = note;

            await _database.SaveTimeEntryAsync(entry);

            // Recalculate the work day for this entry
            var workDay = await _database.GetWorkDayAsync(entry.Timestamp.Date);
            if (workDay != null)
            {
                await _calculation.RecalculateWorkDayAsync(workDay);
            }
        }
    }

    public async Task UpdatePauseEntryAsync(int pauseId, DateTime newStart, DateTime newEnd)
    {
        var today = await GetTodayAsync();
        var pauses = await _database.GetPauseEntriesAsync(today.Id);
        var pause = pauses.FirstOrDefault(p => p.Id == pauseId);

        if (pause != null)
        {
            pause.StartTime = newStart;
            pause.EndTime = newEnd;
            await _database.SavePauseEntryAsync(pause);
            await _calculation.RecalculatePauseTimeAsync(today);
            await _calculation.RecalculateWorkDayAsync(today);
        }
    }

    public async Task<TrackingStatus> GetCurrentStatusAsync()
    {
        await LoadStatusAsync();
        return CurrentStatus;
    }

    public TimeSpan GetCurrentSessionDuration()
    {
        if (CurrentStatus == TrackingStatus.Idle)
            return TimeSpan.Zero;

        // Non-blocking: Return cached value (updated by GetCurrentWorkTimeAsync)
        return _cachedWorkTime;
    }
}
