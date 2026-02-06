using System.Globalization;
using SQLite;

namespace ZeitManager.Models;

[Table("ShiftExceptions")]
public class ShiftException
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ShiftScheduleId { get; set; }

    [Indexed]
    public string Date { get; set; } = DateOnly.FromDateTime(DateTime.Today).ToString("O");

    public ExceptionType ExceptionType { get; set; }
    public ShiftType? NewShiftType { get; set; }
    public string? Note { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    // Computed properties (not in DB)

    [Ignore]
    public DateOnly DateValue
    {
        get => DateOnly.Parse(Date, CultureInfo.InvariantCulture);
        set => Date = value.ToString("O");
    }

    [Ignore]
    public bool IsFreeDay => ExceptionType is ExceptionType.Vacation or ExceptionType.Sick;
}
