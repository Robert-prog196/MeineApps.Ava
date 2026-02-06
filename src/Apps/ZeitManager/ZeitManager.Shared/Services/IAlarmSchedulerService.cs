using ZeitManager.Models;

namespace ZeitManager.Services;

public interface IAlarmSchedulerService
{
    Task InitializeAsync();
    Task ScheduleAlarmAsync(AlarmItem alarm);
    Task CancelAlarmAsync(AlarmItem alarm);
    Task SnoozeAlarmAsync(AlarmItem alarm);
    Task DismissAlarmAsync(AlarmItem alarm);

    event EventHandler<AlarmItem>? AlarmTriggered;
}
