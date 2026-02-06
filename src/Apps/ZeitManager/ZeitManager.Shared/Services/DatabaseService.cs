using SQLite;
using ZeitManager.Models;

namespace ZeitManager.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DatabaseService()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _dbPath = Path.Combine(folder, "zeitmanager.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_database != null) return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<TimerItem>();
            await _database.CreateTableAsync<AlarmItem>();
            await _database.CreateTableAsync<ShiftSchedule>();
            await _database.CreateTableAsync<ShiftException>();
            await _database.CreateTableAsync<CustomShiftPattern>();
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database == null) await InitializeAsync();
        return _database!;
    }

    // Timers
    public async Task<List<TimerItem>> GetTimersAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimerItem>().ToListAsync();
    }

    public async Task<TimerItem?> GetTimerAsync(int id)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimerItem>().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<int> SaveTimerAsync(TimerItem timer)
    {
        var db = await GetDatabaseAsync();
        if (timer.Id != 0)
            return await db.UpdateAsync(timer);
        return await db.InsertAsync(timer);
    }

    public async Task<int> DeleteTimerAsync(TimerItem timer)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(timer);
    }

    // Alarms
    public async Task<List<AlarmItem>> GetAlarmsAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<AlarmItem>().ToListAsync();
    }

    public async Task<AlarmItem?> GetAlarmAsync(int id)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<AlarmItem>().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<int> SaveAlarmAsync(AlarmItem alarm)
    {
        var db = await GetDatabaseAsync();
        if (alarm.Id != 0)
            return await db.UpdateAsync(alarm);
        return await db.InsertAsync(alarm);
    }

    public async Task<int> DeleteAlarmAsync(AlarmItem alarm)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(alarm);
    }

    // Shift Schedules
    public async Task<List<ShiftSchedule>> GetShiftSchedulesAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<ShiftSchedule>().ToListAsync();
    }

    public async Task<ShiftSchedule?> GetActiveScheduleAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<ShiftSchedule>().FirstOrDefaultAsync(s => s.IsActive);
    }

    public async Task<int> SaveShiftScheduleAsync(ShiftSchedule schedule)
    {
        var db = await GetDatabaseAsync();
        if (schedule.Id != 0)
            return await db.UpdateAsync(schedule);
        return await db.InsertAsync(schedule);
    }

    public async Task<int> DeleteShiftScheduleAsync(ShiftSchedule schedule)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(schedule);
    }

    // Shift Exceptions
    public async Task<List<ShiftException>> GetShiftExceptionsAsync(int scheduleId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<ShiftException>()
            .Where(e => e.ShiftScheduleId == scheduleId)
            .ToListAsync();
    }

    public async Task<int> SaveShiftExceptionAsync(ShiftException exception)
    {
        var db = await GetDatabaseAsync();
        if (exception.Id != 0)
            return await db.UpdateAsync(exception);
        return await db.InsertAsync(exception);
    }

    public async Task<int> DeleteShiftExceptionAsync(ShiftException exception)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(exception);
    }
}
