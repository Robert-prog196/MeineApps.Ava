using System.Globalization;
using SQLite;

namespace ZeitManager.Models;

[Table("ShiftSchedules")]
public class ShiftSchedule
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public ShiftPatternType PatternType { get; set; } = ShiftPatternType.FifteenShift;
    public int ShiftGroupNumber { get; set; } = 1;
    public string StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today).ToString("O");
    public bool IsActive { get; set; }
    public long EarlyShiftWakeTimeTicks { get; set; } = new TimeOnly(5, 0).Ticks;
    public long LateShiftWakeTimeTicks { get; set; } = new TimeOnly(12, 0).Ticks;
    public long NightShiftWakeTimeTicks { get; set; } = new TimeOnly(20, 0).Ticks;
    public int WakeUpMinutesBefore { get; set; } = 60;
    public int? CustomPatternId { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    // Computed properties (not in DB)

    [Ignore]
    public DateOnly StartDateValue
    {
        get => DateOnly.Parse(StartDate, CultureInfo.InvariantCulture);
        set => StartDate = value.ToString("O");
    }

    [Ignore]
    public TimeOnly EarlyShiftWakeTime
    {
        get => new(EarlyShiftWakeTimeTicks);
        set => EarlyShiftWakeTimeTicks = value.Ticks;
    }

    [Ignore]
    public TimeOnly LateShiftWakeTime
    {
        get => new(LateShiftWakeTimeTicks);
        set => LateShiftWakeTimeTicks = value.Ticks;
    }

    [Ignore]
    public TimeOnly NightShiftWakeTime
    {
        get => new(NightShiftWakeTimeTicks);
        set => NightShiftWakeTimeTicks = value.Ticks;
    }

    [Ignore]
    public int MaxGroupNumber => PatternType switch
    {
        ShiftPatternType.FifteenShift => 3,
        ShiftPatternType.TwentyOneShift => 5,
        ShiftPatternType.Custom => 10,
        _ => 3
    };
}
