namespace MeineApps.CalcLib;

/// <summary>
/// Repräsentiert einen Eintrag im Berechnungsverlauf.
/// </summary>
public record CalculationHistoryEntry(
    string Expression,      // z.B. "5 + 3"
    string Result,          // z.B. "8"
    double ResultValue,     // z.B. 8.0
    DateTime Timestamp
);
