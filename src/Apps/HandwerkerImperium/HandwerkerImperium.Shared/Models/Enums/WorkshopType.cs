namespace HandwerkerImperium.Models.Enums;

/// <summary>
/// Types of workshops/trades in the game.
/// Each type has unique mini-games and progression.
/// </summary>
public enum WorkshopType
{
    /// <summary>Woodworking - Sawing, Planing, Assembly</summary>
    Carpenter = 0,

    /// <summary>Plumbing - Pipe puzzles, Fittings</summary>
    Plumber = 1,

    /// <summary>Electrical - Wiring, Circuits</summary>
    Electrician = 2,

    /// <summary>Painting - Brush strokes, Color mixing</summary>
    Painter = 3,

    /// <summary>Roofing - Tile laying, Measurements</summary>
    Roofer = 4,

    /// <summary>General Contractor - Large projects, Management</summary>
    Contractor = 5
}

/// <summary>
/// Extension methods for WorkshopType.
/// </summary>
public static class WorkshopTypeExtensions
{
    /// <summary>
    /// Gets the player level required to unlock this workshop.
    /// </summary>
    public static int GetUnlockLevel(this WorkshopType type) => type switch
    {
        WorkshopType.Carpenter => 1,      // Available from start
        WorkshopType.Plumber => 6,        // Unlocks at level 6
        WorkshopType.Electrician => 11,   // Unlocks at level 11
        WorkshopType.Painter => 16,       // Unlocks at level 16
        WorkshopType.Roofer => 21,        // Unlocks at level 21
        WorkshopType.Contractor => 26,    // Unlocks at level 26
        _ => 1
    };

    /// <summary>
    /// Gets the icon/emoji for this workshop type.
    /// </summary>
    public static string GetIcon(this WorkshopType type) => type switch
    {
        WorkshopType.Carpenter => "\ud83e\ude9a",
        WorkshopType.Plumber => "\ud83d\udd27",
        WorkshopType.Electrician => "\u26a1",
        WorkshopType.Painter => "\ud83c\udfa8",
        WorkshopType.Roofer => "\ud83c\udfe0",
        WorkshopType.Contractor => "\ud83c\udfd7\ufe0f",
        _ => "\ud83d\udd27"
    };

    /// <summary>
    /// Gets the localization key for this workshop type.
    /// </summary>
    public static string GetLocalizationKey(this WorkshopType type) => type switch
    {
        WorkshopType.Carpenter => "Carpenter",
        WorkshopType.Plumber => "Plumber",
        WorkshopType.Electrician => "Electrician",
        WorkshopType.Painter => "Painter",
        WorkshopType.Roofer => "Roofer",
        WorkshopType.Contractor => "Contractor",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the base income multiplier for this workshop type.
    /// Higher-tier workshops earn more per worker.
    /// </summary>
    public static decimal GetBaseIncomeMultiplier(this WorkshopType type) => type switch
    {
        WorkshopType.Carpenter => 1.0m,
        WorkshopType.Plumber => 1.5m,
        WorkshopType.Electrician => 2.0m,
        WorkshopType.Painter => 2.5m,
        WorkshopType.Roofer => 3.0m,
        WorkshopType.Contractor => 4.0m,
        _ => 1.0m
    };
}
