using System.Timers;
using Avalonia.Threading;
using ZeitManager.Models;
using Timer = System.Timers.Timer;

namespace ZeitManager.Services;

public class AlarmSchedulerService : IAlarmSchedulerService, IDisposable
{
    private readonly IDatabaseService _database;
    private Timer? _checkTimer;
    private readonly List<AlarmItem> _activeAlarms = [];
    private readonly HashSet<int> _triggeredToday = [];
    private DateOnly _lastTriggerDate = DateOnly.MinValue;

    public event EventHandler<AlarmItem>? AlarmTriggered;

    public AlarmSchedulerService(IDatabaseService database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        var alarms = await _database.GetAlarmsAsync();
        _activeAlarms.Clear();
        _activeAlarms.AddRange(alarms.Where(a => a.IsEnabled && !a.IsShiftAlarm));
        EnsureCheckTimer();
    }

    public async Task ScheduleAlarmAsync(AlarmItem alarm)
    {
        _activeAlarms.RemoveAll(a => a.Id == alarm.Id);
        if (alarm.IsEnabled)
        {
            alarm.CurrentSnoozeCount = 0;
            _activeAlarms.Add(alarm);
        }
        await _database.SaveAlarmAsync(alarm);
        EnsureCheckTimer();
    }

    public async Task CancelAlarmAsync(AlarmItem alarm)
    {
        _activeAlarms.RemoveAll(a => a.Id == alarm.Id);
        alarm.CurrentSnoozeCount = 0;
        await _database.SaveAlarmAsync(alarm);
        CheckStopTimer();
    }

    public async Task SnoozeAlarmAsync(AlarmItem alarm)
    {
        if (alarm.CurrentSnoozeCount >= alarm.MaxSnoozeCount) return;

        alarm.CurrentSnoozeCount++;
        alarm.NextTriggerDateTime = DateTime.Now.AddMinutes(alarm.SnoozeDurationMinutes);
        await _database.SaveAlarmAsync(alarm);

        if (!_activeAlarms.Any(a => a.Id == alarm.Id))
            _activeAlarms.Add(alarm);
        EnsureCheckTimer();
    }

    public async Task DismissAlarmAsync(AlarmItem alarm)
    {
        alarm.CurrentSnoozeCount = 0;
        alarm.NextTriggerDateTime = null;

        if (!alarm.IsRepeating)
        {
            alarm.IsEnabled = false;
            _activeAlarms.RemoveAll(a => a.Id == alarm.Id);
        }

        await _database.SaveAlarmAsync(alarm);
    }

    private void EnsureCheckTimer()
    {
        if (_checkTimer != null) return;
        _checkTimer = new Timer(30_000); // Check every 30 seconds
        _checkTimer.Elapsed += OnCheckTimerTick;
        _checkTimer.Start();
    }

    private void CheckStopTimer()
    {
        if (_activeAlarms.Count == 0 && _checkTimer != null)
        {
            _checkTimer.Stop();
            _checkTimer.Dispose();
            _checkTimer = null;
        }
    }

    private void OnCheckTimerTick(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            // Reset triggered set on new day
            if (today != _lastTriggerDate)
            {
                _triggeredToday.Clear();
                _lastTriggerDate = today;
            }

            foreach (var alarm in _activeAlarms.ToList())
            {
                if (ShouldTrigger(alarm, now))
                {
                    // Prevent double-trigger: skip if already triggered this minute (unless snoozed)
                    var triggerKey = alarm.NextTriggerDateTime != null
                        ? alarm.Id * 10000 + now.Hour * 100 + now.Minute + alarm.CurrentSnoozeCount
                        : alarm.Id * 10000 + now.Hour * 100 + now.Minute;

                    if (_triggeredToday.Contains(triggerKey)) continue;
                    _triggeredToday.Add(triggerKey);

                    // Clear snooze after trigger
                    alarm.NextTriggerDateTime = null;

                    AlarmTriggered?.Invoke(this, alarm);
                }
            }
        });
    }

    private static bool ShouldTrigger(AlarmItem alarm, DateTime now)
    {
        // Check snoozed alarm
        if (alarm.NextTriggerDateTime != null)
        {
            return now >= alarm.NextTriggerDateTime.Value;
        }

        // Check regular alarm time
        var alarmTime = alarm.Time;
        if (now.Hour != alarmTime.Hour || now.Minute != alarmTime.Minute)
            return false;

        // Only trigger within first 30 seconds of the minute
        if (now.Second > 30)
            return false;

        // Check weekday
        if (alarm.IsRepeating)
        {
            var todayFlag = now.DayOfWeek switch
            {
                DayOfWeek.Monday => WeekDays.Monday,
                DayOfWeek.Tuesday => WeekDays.Tuesday,
                DayOfWeek.Wednesday => WeekDays.Wednesday,
                DayOfWeek.Thursday => WeekDays.Thursday,
                DayOfWeek.Friday => WeekDays.Friday,
                DayOfWeek.Saturday => WeekDays.Saturday,
                DayOfWeek.Sunday => WeekDays.Sunday,
                _ => WeekDays.None
            };
            return alarm.RepeatDaysEnum.HasFlag(todayFlag);
        }

        return true;
    }

    public void Dispose()
    {
        _checkTimer?.Stop();
        _checkTimer?.Dispose();
        _checkTimer = null;
    }
}
