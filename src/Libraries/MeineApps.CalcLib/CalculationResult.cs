namespace MeineApps.CalcLib;

/// <summary>
/// Ergebnis einer Berechnung mit optionalem Fehler
/// </summary>
public readonly record struct CalculationResult
{
    public double Value { get; init; }
    public bool IsError { get; init; }
    public string? ErrorMessage { get; init; }

    public static CalculationResult Success(double value) => new()
    {
        Value = value,
        IsError = false,
        ErrorMessage = null
    };

    public static CalculationResult Error(string message) => new()
    {
        Value = double.NaN,
        IsError = true,
        ErrorMessage = message
    };
}
