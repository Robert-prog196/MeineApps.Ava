using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Spezialisierungstyp für Workshops (ab Level 100).
/// </summary>
public enum SpecializationType
{
    /// <summary>+30% Einkommen, -10% Worker-Kapazität.</summary>
    Efficiency,
    /// <summary>+20% Worker-Effizienz, +15% Kosten.</summary>
    Quality,
    /// <summary>-25% Kosten, -5% Einkommen.</summary>
    Economy
}

/// <summary>
/// Eine gewählte Workshop-Spezialisierung.
/// </summary>
public class WorkshopSpecialization
{
    [JsonPropertyName("type")]
    public SpecializationType Type { get; set; }

    [JsonIgnore]
    public decimal IncomeModifier => Type switch
    {
        SpecializationType.Efficiency => 0.30m,
        SpecializationType.Quality => 0m,
        SpecializationType.Economy => -0.05m,
        _ => 0m
    };

    [JsonIgnore]
    public decimal CostModifier => Type switch
    {
        SpecializationType.Efficiency => 0m,
        SpecializationType.Quality => 0.15m,
        SpecializationType.Economy => -0.25m,
        _ => 0m
    };

    [JsonIgnore]
    public decimal EfficiencyModifier => Type switch
    {
        SpecializationType.Efficiency => 0m,
        SpecializationType.Quality => 0.20m,
        SpecializationType.Economy => 0m,
        _ => 0m
    };

    [JsonIgnore]
    public int WorkerCapacityModifier => Type switch
    {
        SpecializationType.Efficiency => -1,  // -10% → ca. 1 weniger
        _ => 0
    };

    [JsonIgnore]
    public string NameKey => $"Specialization_{Type}";

    [JsonIgnore]
    public string DescriptionKey => $"Specialization_{Type}_Desc";

    [JsonIgnore]
    public string Color => Type switch
    {
        SpecializationType.Efficiency => "#FF9800",
        SpecializationType.Quality => "#2196F3",
        SpecializationType.Economy => "#4CAF50",
        _ => "#808080"
    };
}
