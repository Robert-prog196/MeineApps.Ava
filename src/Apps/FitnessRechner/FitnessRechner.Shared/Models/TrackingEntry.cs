namespace FitnessRechner.Models;

/// <summary>
/// Ein einzelner Tracking-Eintrag (Gewicht, BMI, Wasser)
/// </summary>
public class TrackingEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public TrackingType Type { get; set; }
    public double Value { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Art des Tracking-Eintrags
/// </summary>
public enum TrackingType
{
    Weight,     // Gewicht in kg
    Bmi,        // BMI-Wert
    Water,      // Wasser in ml
    BodyFat     // Körperfettanteil in %
}

/// <summary>
/// Statistik für einen Tracking-Typ
/// </summary>
public record TrackingStats(
    TrackingType Type,
    double CurrentValue,
    double AverageValue,
    double MinValue,
    double MaxValue,
    double TrendValue,  // Differenz zum Vortag
    int TotalEntries);
