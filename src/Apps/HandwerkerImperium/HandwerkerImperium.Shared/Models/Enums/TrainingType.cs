namespace HandwerkerImperium.Models.Enums;

/// <summary>
/// Trainings-Typen für Arbeiter.
/// </summary>
public enum TrainingType
{
    /// <summary>XP → Level → +Effizienz (Standard-Training)</summary>
    Efficiency = 0,

    /// <summary>Senkt FatiguePerHour permanent (bis min 50% Reduktion)</summary>
    Endurance = 1,

    /// <summary>Senkt MoodDecayPerHour permanent (bis min 50% Reduktion)</summary>
    Morale = 2
}
