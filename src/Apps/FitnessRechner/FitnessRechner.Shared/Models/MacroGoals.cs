namespace FitnessRechner.Models;

/// <summary>
/// Repräsentiert die täglichen Makronährstoff-Ziele
/// </summary>
public class MacroGoals
{
    public double ProteinGrams { get; set; }
    public double CarbsGrams { get; set; }
    public double FatGrams { get; set; }

    /// <summary>
    /// Berechnet die Gesamtkalorien aus den Makronährstoffen
    /// 1g Protein = 4 kcal, 1g Carbs = 4 kcal, 1g Fat = 9 kcal
    /// </summary>
    public double TotalCalories => (ProteinGrams * 4) + (CarbsGrams * 4) + (FatGrams * 9);

    /// <summary>
    /// Berechnet die Verteilung als Prozentsatz
    /// </summary>
    public MacroDistribution GetDistribution()
    {
        var totalCal = TotalCalories;
        if (totalCal == 0)
            return new MacroDistribution(0, 0, 0);

        var proteinPercent = (ProteinGrams * 4 / totalCal) * 100;
        var carbsPercent = (CarbsGrams * 4 / totalCal) * 100;
        var fatPercent = (FatGrams * 9 / totalCal) * 100;

        return new MacroDistribution(proteinPercent, carbsPercent, fatPercent);
    }
}

/// <summary>
/// Prozentuale Verteilung der Makronährstoffe
/// </summary>
public record MacroDistribution(
    double ProteinPercent,
    double CarbsPercent,
    double FatPercent
);

/// <summary>
/// Fortschritt für einen einzelnen Makronährstoff
/// </summary>
public record MacroProgress(
    string Name,
    double Current,
    double Goal,
    double Remaining,
    double PercentageComplete
)
{
    public bool IsOverGoal => Current > Goal;
}

/// <summary>
/// Tagesübersicht der Makronährstoff-Aufnahme
/// </summary>
public record DailyMacroSummary(
    double TotalProtein,
    double TotalCarbs,
    double TotalFat,
    double TotalCalories,
    MacroGoals Goals
)
{
    public MacroProgress ProteinProgress => new(
        "Protein",
        TotalProtein,
        Goals.ProteinGrams,
        Goals.ProteinGrams - TotalProtein,
        Goals.ProteinGrams > 0 ? (TotalProtein / Goals.ProteinGrams) * 100 : 0
    );

    public MacroProgress CarbsProgress => new(
        "Kohlenhydrate",
        TotalCarbs,
        Goals.CarbsGrams,
        Goals.CarbsGrams - TotalCarbs,
        Goals.CarbsGrams > 0 ? (TotalCarbs / Goals.CarbsGrams) * 100 : 0
    );

    public MacroProgress FatProgress => new(
        "Fett",
        TotalFat,
        Goals.FatGrams,
        Goals.FatGrams - TotalFat,
        Goals.FatGrams > 0 ? (TotalFat / Goals.FatGrams) * 100 : 0
    );

    public MacroProgress CaloriesProgress => new(
        "Kalorien",
        TotalCalories,
        Goals.TotalCalories,
        Goals.TotalCalories - TotalCalories,
        Goals.TotalCalories > 0 ? (TotalCalories / Goals.TotalCalories) * 100 : 0
    );
}
