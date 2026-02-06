namespace MeineApps.Core.Ava.Services;

/// <summary>
/// Service for unit conversion between Metric and Imperial
/// </summary>
public interface IUnitConverterService
{
    UnitSystem CurrentSystem { get; set; }
    event EventHandler? UnitSystemChanged;

    // Length
    double ConvertLength(double meters, bool toDisplay = true);
    string GetLengthUnit(bool abbreviated = true);
    string FormatLength(double meters, int decimals = 2);

    // Area
    double ConvertArea(double squareMeters, bool toDisplay = true);
    string GetAreaUnit(bool abbreviated = true);
    string FormatArea(double squareMeters, int decimals = 2);

    // Volume
    double ConvertVolume(double liters, bool toDisplay = true);
    string GetVolumeUnit(bool abbreviated = true);
    string FormatVolume(double liters, int decimals = 2);

    // Weight
    double ConvertWeight(double kilograms, bool toDisplay = true);
    string GetWeightUnit(bool abbreviated = true);
    string FormatWeight(double kilograms, int decimals = 2);

    // Temperature
    double ConvertTemperature(double celsius, bool toDisplay = true);
    string GetTemperatureUnit(bool abbreviated = true);
    string FormatTemperature(double celsius, int decimals = 1);
}

/// <summary>
/// Unit system enum
/// </summary>
public enum UnitSystem
{
    Metric,
    Imperial
}
