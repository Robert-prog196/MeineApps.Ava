namespace FitnessRechner;

/// <summary>
/// Zentrale Preference-Keys und App-Konstanten.
/// Verhindert Duplikation und Typo-Bugs über mehrere ViewModels hinweg.
/// </summary>
public static class PreferenceKeys
{
    // Ziel-Einstellungen
    public const string CalorieGoal = "daily_calorie_goal";
    public const string WaterGoal = "daily_water_goal";
    public const string WeightGoal = "weight_goal";
    public const string MacroProteinGoal = "macro_goal_protein";
    public const string MacroCarbsGoal = "macro_goal_carbs";
    public const string MacroFatGoal = "macro_goal_fat";

    // Erweiterte Food-DB
    public const string ExtendedFoodDbExpiry = "ExtendedFoodDbExpiry";

    // Scan-Limit
    public const string ScanLimitCount = "ScanLimit_Count";
    public const string ScanLimitDate = "ScanLimit_Date";

    // Streak
    public const string StreakCurrent = "streak_current";
    public const string StreakBest = "streak_best";
    public const string StreakLastLogDate = "streak_last_log_date";

    // Chart
    public const string ChartDays = "chart_days";
    public const int DefaultChartDays = 30;

    // UI-Konstanten
    public const int UndoTimeoutMs = 5000;
}
