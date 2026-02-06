using ZeitManager.Models;

namespace ZeitManager.Services;

public interface IShiftScheduleService
{
    Task<ShiftSchedule?> GetActiveScheduleAsync();
    Task<List<ShiftSchedule>> GetAllSchedulesAsync();
    Task SaveScheduleAsync(ShiftSchedule schedule);
    Task DeleteScheduleAsync(ShiftSchedule schedule);
    Task ActivateScheduleAsync(ShiftSchedule schedule);
    Task DeactivateScheduleAsync(ShiftSchedule schedule);

    Task<List<ShiftException>> GetExceptionsAsync(int scheduleId);
    Task SaveExceptionAsync(ShiftException exception);
    Task DeleteExceptionAsync(ShiftException exception);

    ShiftType GetShiftForDate(ShiftSchedule schedule, DateOnly date);
    ShiftType GetShiftForDate(ShiftSchedule schedule, DateOnly date, List<ShiftException> exceptions);
}
