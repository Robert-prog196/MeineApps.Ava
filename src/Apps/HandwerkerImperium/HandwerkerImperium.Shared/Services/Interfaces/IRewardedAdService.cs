namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Service for rewarded video ads.
/// </summary>
public interface IRewardedAdService
{
    /// <summary>
    /// Whether a rewarded ad is currently loaded and ready to show.
    /// </summary>
    bool IsRewardedAdReady { get; }

    /// <summary>
    /// Whether rewarded ads are available (not disabled by premium).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initializes the rewarded ad service.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Loads a new rewarded ad.
    /// </summary>
    Task LoadAdAsync();

    /// <summary>
    /// Shows the rewarded ad. Returns true if shown successfully and reward earned.
    /// </summary>
    Task<bool> ShowAdAsync();

    /// <summary>
    /// Disables rewarded ads (after premium purchase).
    /// </summary>
    void Disable();
}
