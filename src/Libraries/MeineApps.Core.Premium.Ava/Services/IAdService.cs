namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// Service for ad management (AdMob)
/// </summary>
public interface IAdService
{
    /// <summary>
    /// Whether ads are enabled (false if premium purchased)
    /// </summary>
    bool AdsEnabled { get; }

    /// <summary>
    /// Whether the banner ad is currently visible
    /// </summary>
    bool BannerVisible { get; }

    /// <summary>
    /// Initialize the ad service with the banner ID
    /// </summary>
    void Initialize(string bannerId);

    /// <summary>
    /// Show the banner
    /// </summary>
    void ShowBanner();

    /// <summary>
    /// Hide the banner
    /// </summary>
    void HideBanner();

    /// <summary>
    /// Disable ads (after premium purchase)
    /// </summary>
    void DisableAds();

    /// <summary>
    /// Event fired when ads state changes
    /// </summary>
    event EventHandler? AdsStateChanged;
}
