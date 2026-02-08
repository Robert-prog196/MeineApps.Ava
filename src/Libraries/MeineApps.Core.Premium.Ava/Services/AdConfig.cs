namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// AdMob Konfiguration fuer alle Apps.
/// Alle Apps nutzen Publisher-Account ca-app-pub-2588160251469436.
/// Jede App hat mehrere Rewarded Ad-Unit-IDs fuer verschiedene Placements.
/// </summary>
public static class AdConfig
{
    // Test Ad Unit IDs (fuer Entwicklung)
    public static class Test
    {
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        public const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    }

    // Production Ad Unit IDs

    public static class RechnerPlus
    {
        // Werbefrei - keine Ads
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/420594339";
    }

    public static class ZeitManager
    {
        // Werbefrei - keine Ads
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/1020047866";
    }

    public static class HandwerkerRechner
    {
        public const string AppId = "ca-app-pub-2588160251469436~1938872706";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/2627273757";
        // Rewarded Ad Placements
        public const string RewardedExtendedHistory = "ca-app-pub-2588160251469436/6068740844";
        public const string RewardedPremiumAccess = "ca-app-pub-2588160251469436/2014913097";
        public const string RewardedProjectExport = "ca-app-pub-2588160251469436/8243171620";
        public const string RewardedMaterialPdf = "ca-app-pub-2588160251469436/5190252147";
    }

    public static class FinanzRechner
    {
        public const string AppId = "ca-app-pub-2588160251469436~8528331789";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/3301929804";
        // Rewarded Ad Placements
        public const string RewardedBudgetAnalysis = "ca-app-pub-2588160251469436/5671834572";
        public const string RewardedExtendedStats = "ca-app-pub-2588160251469436/9554899549";
        public const string RewardedExportPdf = "ca-app-pub-2588160251469436/3424934485";
        public const string RewardedExportCsv = "ca-app-pub-2588160251469436/1356438259";
    }

    public static class FitnessRechner
    {
        public const string AppId = "ca-app-pub-2588160251469436~1827192061";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/9276806163";
        // Rewarded Ad Placements
        public const string RewardedBarcodeScan = "ca-app-pub-2588160251469436/7073938526";
        public const string RewardedDetailAnalysis = "ca-app-pub-2588160251469436/6443957290";
        public const string RewardedExtendedFoodDb = "ca-app-pub-2588160251469436/9527063494";
        public const string RewardedTrackingExport = "ca-app-pub-2588160251469436/9681815382";
    }

    public static class WorkTimePro
    {
        public const string AppId = "ca-app-pub-2588160251469436~9866108383";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/8916171963";
        // Rewarded Ad Placements
        public const string RewardedExport = "ca-app-pub-2588160251469436/4893502974";
        public const string RewardedMonthlyStats = "ca-app-pub-2588160251469436/6900114506";
        public const string RewardedVacationEntry = "ca-app-pub-2588160251469436/1747320492";
    }

    public static class BomberBlast
    {
        public const string AppId = "ca-app-pub-2588160251469436~8809763733";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/5894462078";
        // Rewarded Ad Placements
        public const string RewardedContinue = "ca-app-pub-2588160251469436/1459117529";
        public const string RewardedLevelSkip = "ca-app-pub-2588160251469436/6792383855";
        public const string RewardedPowerUp = "ca-app-pub-2588160251469436/8246016521";
        public const string RewardedScoreDouble = "ca-app-pub-2588160251469436/6242669514";
    }

    public static class HandwerkerImperium
    {
        public const string AppId = "ca-app-pub-2588160251469436~3907946957";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/5062090417";
        public const string InterstitialAdUnitId = "ca-app-pub-2588160251469436/3553567622";
        // Rewarded: HandwerkerImperium nutzt aktuell keine separaten Placements
        public const string RewardedDefault = "";
    }

    /// <summary>Banner Ad-Unit-ID je nach Build-Konfiguration</summary>
    public static string GetBannerAdUnitId(string appName)
    {
#if DEBUG
        return Test.BannerAdUnitId;
#else
        return appName switch
        {
            "RechnerPlus" => RechnerPlus.BannerAdUnitId,
            "ZeitManager" => ZeitManager.BannerAdUnitId,
            "HandwerkerRechner" => HandwerkerRechner.BannerAdUnitId,
            "FinanzRechner" => FinanzRechner.BannerAdUnitId,
            "FitnessRechner" => FitnessRechner.BannerAdUnitId,
            "WorkTimePro" => WorkTimePro.BannerAdUnitId,
            "BomberBlast" => BomberBlast.BannerAdUnitId,
            "HandwerkerImperium" => HandwerkerImperium.BannerAdUnitId,
            _ => Test.BannerAdUnitId
        };
#endif
    }

    /// <summary>
    /// Rewarded Ad-Unit-ID fuer ein bestimmtes Placement.
    /// Im DEBUG-Modus wird immer die Test-ID verwendet.
    /// </summary>
    /// <param name="appName">App-Name (z.B. "BomberBlast")</param>
    /// <param name="placement">Placement-Name (z.B. "continue", "score_double"). Null = Default.</param>
    public static string GetRewardedAdUnitId(string appName, string? placement = null)
    {
#if DEBUG
        return Test.RewardedAdUnitId;
#else
        var id = (appName, placement) switch
        {
            // BomberBlast
            ("BomberBlast", "continue") => BomberBlast.RewardedContinue,
            ("BomberBlast", "level_skip") => BomberBlast.RewardedLevelSkip,
            ("BomberBlast", "power_up") => BomberBlast.RewardedPowerUp,
            ("BomberBlast", "score_double") => BomberBlast.RewardedScoreDouble,
            ("BomberBlast", _) => BomberBlast.RewardedContinue,

            // FinanzRechner
            ("FinanzRechner", "export_pdf") => FinanzRechner.RewardedExportPdf,
            ("FinanzRechner", "export_csv") => FinanzRechner.RewardedExportCsv,
            ("FinanzRechner", "budget_analysis") => FinanzRechner.RewardedBudgetAnalysis,
            ("FinanzRechner", "extended_stats") => FinanzRechner.RewardedExtendedStats,
            ("FinanzRechner", _) => FinanzRechner.RewardedExportPdf,

            // HandwerkerRechner
            ("HandwerkerRechner", "premium_access") => HandwerkerRechner.RewardedPremiumAccess,
            ("HandwerkerRechner", "material_pdf") => HandwerkerRechner.RewardedMaterialPdf,
            ("HandwerkerRechner", "project_export") => HandwerkerRechner.RewardedProjectExport,
            ("HandwerkerRechner", "extended_history") => HandwerkerRechner.RewardedExtendedHistory,
            ("HandwerkerRechner", _) => HandwerkerRechner.RewardedPremiumAccess,

            // FitnessRechner
            ("FitnessRechner", "barcode_scan") => FitnessRechner.RewardedBarcodeScan,
            ("FitnessRechner", "detail_analysis") => FitnessRechner.RewardedDetailAnalysis,
            ("FitnessRechner", "extended_food_db") => FitnessRechner.RewardedExtendedFoodDb,
            ("FitnessRechner", "tracking_export") => FitnessRechner.RewardedTrackingExport,
            ("FitnessRechner", _) => FitnessRechner.RewardedBarcodeScan,

            // WorkTimePro
            ("WorkTimePro", "export") => WorkTimePro.RewardedExport,
            ("WorkTimePro", "monthly_stats") => WorkTimePro.RewardedMonthlyStats,
            ("WorkTimePro", "vacation_entry") => WorkTimePro.RewardedVacationEntry,
            ("WorkTimePro", _) => WorkTimePro.RewardedExport,

            // HandwerkerImperium
            ("HandwerkerImperium", _) => HandwerkerImperium.RewardedDefault,

            _ => ""
        };
        // Fallback auf Test-ID wenn noch keine echte ID konfiguriert
        return string.IsNullOrEmpty(id) ? Test.RewardedAdUnitId : id;
#endif
    }
}
