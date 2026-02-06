namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// AdMob configuration for all apps
/// </summary>
public static class AdConfig
{
    // Test Ad Unit IDs (for development)
    public static class Test
    {
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        public const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    }

    // Production Ad Unit IDs
    public static class RechnerPlus
    {
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/420594339";
    }

    public static class ZeitManager
    {
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/1020047866";
    }

    public static class HandwerkerRechner
    {
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/6519786556";
    }

    public static class FinanzRechner
    {
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/6463946234";
    }

    public static class FitnessRechner
    {
        public const string BannerAdUnitId = "ca-app-pub-2667921454778639/5165292531";
    }

    public static class WorkTimePro
    {
        public const string AppId = "ca-app-pub-2588160251469436~9866108383";
        public const string BannerAdUnitId = "ca-app-pub-2588160251469436/8991611030";
    }

    public static class BomberBlast
    {
        public const string AppId = "ca-app-pub-3940256099942544~3347511713"; // Test ID - Replace with real ID
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // Test ID - Replace with real ID
        public const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // Test ID
        public const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Test ID
    }

    public static class HandwerkerImperium
    {
        public const string AppId = "ca-app-pub-3940256099942544~3347511713"; // Test ID - Replace with real ID
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // Test ID - Replace with real ID
        public const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Test ID
    }

    /// <summary>
    /// Get the banner ad unit ID based on build configuration
    /// </summary>
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
}
