namespace SocialPostGenerator;

/// <summary>
/// App-Definition mit allen Infos für Post-Generierung
/// </summary>
record AppInfo(
    string Name,
    string PackageId,
    string AccentColorHex,
    string Type,
    string Price,
    bool HasAds,
    string[] KeyFeatures,
    string ShortDescription,
    string[] Tags,
    string TesterLink
);

/// <summary>
/// Registry mit allen 8 Apps und ihren Metadaten
/// </summary>
static class AppRegistry
{
    // Tester-Community Link
    const string TesterGroupLink = "https://groups.google.com/g/testersrsdigital/c/MoRF6WB_pCE/m/wWrN43yqAAAJ";

    public static AppInfo[] GetAll() =>
    [
        new(
            "RechnerPlus", "com.meineapps.rechnerplus",
            "#3949AB", "scientific calculator & unit converter",
            "Free, no ads", false,
            [
                "scientific & basic mode with live preview",
                "11 unit converter categories",
                "calculation history with undo/redo",
                "landscape layout auto-switches to scientific",
                "expression syntax highlighting"
            ],
            "Scientific calculator with 11-category unit converter. Completely free, no ads.",
            ["calculator", "unitconverter", "freeapp", "productivity"],
            TesterGroupLink
        ),

        new(
            "ZeitManager", "com.meineapps.zeitmanager",
            "#22D3EE", "timer, stopwatch, pomodoro & alarm app",
            "Free, no ads", false,
            [
                "multi-timer with quick-timer presets",
                "stopwatch with lap times & best/worst marking",
                "pomodoro with focus statistics & streak",
                "alarm with math & shake challenges",
                "shift planner (15/21-shift patterns)"
            ],
            "Multi-timer, stopwatch, pomodoro, alarm with challenges & shift planner. Free, no ads.",
            ["timer", "pomodoro", "productivity", "freeapp"],
            TesterGroupLink
        ),

        new(
            "FinanzRechner", "com.meineapps.finanzrechner",
            "#22C55E", "expense tracker & finance calculator",
            "Free + $3.99 remove ads", true,
            [
                "expense tracking with categories & budgets",
                "6 finance calculators (compound interest, loan, savings...)",
                "recurring transactions with auto-processing",
                "charts & statistics with PDF/CSV export",
                "budget alerts & over-budget warnings"
            ],
            "Expense tracker with 6 finance calculators, budget management, and export.",
            ["financeapp", "budgettracker", "expensetracker", "productivity"],
            TesterGroupLink
        ),

        new(
            "FitnessRechner", "com.meineapps.fitnessrechner",
            "#E91E63", "fitness calculator & food tracker",
            "Free + $3.99 remove ads", true,
            [
                "5 calculators: BMI, calories, water, ideal weight, body fat",
                "native barcode scanner (CameraX + ML Kit)",
                "food database with 114 items + recipes",
                "weight tracking with charts & goals",
                "gamification: achievements, XP, daily challenges"
            ],
            "Fitness app with 5 calculators, barcode scanner, food tracking & gamification.",
            ["fitnessapp", "caloriecounter", "bmi", "healthapp"],
            TesterGroupLink
        ),

        new(
            "HandwerkerRechner", "com.meineapps.handwerkerrechner",
            "#FF6D00", "construction calculator suite",
            "Free + $3.99 remove ads", true,
            [
                "11 calculators: tile, wallpaper, paint, flooring, concrete...",
                "premium: drywall, electrical, metal, garden, roof/solar, stairs",
                "project management with JSON persistence",
                "material list PDF export",
                "unit converter for length, area, volume, weight"
            ],
            "11 construction calculators with project management and PDF export.",
            ["construction", "diy", "toolapp", "craftsman"],
            TesterGroupLink
        ),

        new(
            "WorkTimePro", "com.meineapps.worktimepro",
            "#009688", "time tracking & work management",
            "Free + $3.99/mo or $19.99 lifetime", true,
            [
                "check-in/out with break management & auto-pause",
                "calendar heatmap with 9 status types",
                "statistics with PDF, Excel & CSV export",
                "vacation management for 16 German states",
                "smart notifications: 5 reminder types"
            ],
            "Time tracking with breaks, calendar heatmap, export, vacation & shift planning.",
            ["timetracking", "worklife", "freelancer", "productivity"],
            TesterGroupLink
        ),

        new(
            "HandwerkerImperium", "com.meineapps.handwerkerimperium",
            "#9C27B0", "idle tycoon game",
            "Free + $4.99 premium", true,
            [
                "8 workshop types (carpentry to general contractor)",
                "8 unique mini-games (sawing, pipe puzzle, wiring...)",
                "45 research upgrades in 3 branches",
                "10 worker tiers (F to Legendary)",
                "prestige system with 3 stages & shop"
            ],
            "Idle tycoon: build your craftsman empire with workshops, mini-games & prestige.",
            ["idlegame", "tycoon", "mobilegaming", "indiegame"],
            TesterGroupLink
        ),

        new(
            "BomberBlast", "org.rsdigital.bomberblast",
            "#FF5252", "bomberman clone",
            "Free + $3.99 remove ads", true,
            [
                "50 story levels in 5 worlds + arcade mode",
                "12 power-up types including kick, line bomb & curse",
                "combo system, slow-motion & screen shake",
                "coin shop with 9 permanent upgrades",
                "2 visual styles: Classic HD & Neon/Cyberpunk"
            ],
            "Bomberman clone with 50 levels, 12 power-ups, combos & 2 visual styles.",
            ["bomberman", "retrogaming", "mobilegaming", "indiegame"],
            TesterGroupLink
        ),
    ];
}
