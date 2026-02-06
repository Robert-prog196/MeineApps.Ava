namespace WorkTimePro.Models;

/// <summary>
/// Zusammenfassung einer Arbeitswoche
/// </summary>
public class WorkWeek
{
    /// <summary>
    /// Kalenderwoche (ISO 8601)
    /// </summary>
    public int WeekNumber { get; set; }

    /// <summary>
    /// Jahr
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Erster Tag der Woche (Montag)
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Letzter Tag der Woche (Sonntag)
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Soll-Arbeitszeit in Minuten (z.B. 2400 = 40h)
    /// </summary>
    public int TargetWorkMinutes { get; set; } = 2400;

    /// <summary>
    /// Tatsächliche Arbeitszeit in Minuten
    /// </summary>
    public int ActualWorkMinutes { get; set; }

    /// <summary>
    /// Gesamte Pausenzeit in Minuten
    /// </summary>
    public int TotalPauseMinutes { get; set; }

    /// <summary>
    /// Saldo in Minuten (Ist - Soll)
    /// </summary>
    public int BalanceMinutes { get; set; }

    /// <summary>
    /// Anzahl gearbeiteter Tage
    /// </summary>
    public int WorkedDays { get; set; }

    /// <summary>
    /// Urlaubstage in dieser Woche
    /// </summary>
    public int VacationDays { get; set; }

    /// <summary>
    /// Krankheitstage in dieser Woche
    /// </summary>
    public int SickDays { get; set; }

    /// <summary>
    /// Feiertage in dieser Woche
    /// </summary>
    public int HolidayDays { get; set; }

    /// <summary>
    /// Liste der einzelnen Tage
    /// </summary>
    public List<WorkDay> Days { get; set; } = new();

    // === Berechnete Properties ===

    /// <summary>
    /// Soll-Arbeitszeit als TimeSpan
    /// </summary>
    public TimeSpan TargetWorkTime => TimeSpan.FromMinutes(TargetWorkMinutes);

    /// <summary>
    /// Tatsächliche Arbeitszeit als TimeSpan
    /// </summary>
    public TimeSpan ActualWorkTime => TimeSpan.FromMinutes(ActualWorkMinutes);

    /// <summary>
    /// Saldo als TimeSpan
    /// </summary>
    public TimeSpan Balance => TimeSpan.FromMinutes(BalanceMinutes);

    /// <summary>
    /// Fortschritt in Prozent (0-100, kann über 100 gehen)
    /// </summary>
    public double ProgressPercent
    {
        get
        {
            if (TargetWorkMinutes == 0) return 0;
            return Math.Min(100, (ActualWorkMinutes * 100.0) / TargetWorkMinutes);
        }
    }

    /// <summary>
    /// Formatierter Zeitraum (z.B. "20.01. - 26.01.")
    /// </summary>
    public string DateRangeDisplay => $"{StartDate:dd.MM.} - {EndDate:dd.MM.}";

    /// <summary>
    /// Formatierte Woche (z.B. "KW 4 / 2026")
    /// </summary>
    public string WeekDisplay => $"KW {WeekNumber} / {Year}";

    /// <summary>
    /// Formatierte Soll-Zeit
    /// </summary>
    public string TargetWorkDisplay => FormatTimeSpan(TargetWorkTime);

    /// <summary>
    /// Formatierte Ist-Zeit
    /// </summary>
    public string ActualWorkDisplay => FormatTimeSpan(ActualWorkTime);

    /// <summary>
    /// Formatierter Saldo (mit +/-)
    /// </summary>
    public string BalanceDisplay
    {
        get
        {
            var prefix = BalanceMinutes >= 0 ? "+" : "";
            return $"{prefix}{FormatTimeSpan(Balance)}";
        }
    }

    /// <summary>
    /// Farbe für Saldo
    /// </summary>
    public string BalanceColor => BalanceMinutes >= 0 ? "#4CAF50" : "#F44336";

    private static string FormatTimeSpan(TimeSpan ts)
    {
        var totalHours = (int)Math.Abs(ts.TotalHours);
        var minutes = Math.Abs(ts.Minutes);
        var sign = ts.TotalMinutes < 0 ? "-" : "";
        return $"{sign}{totalHours}:{minutes:D2}";
    }
}
