using MeineApps.Core.Premium.Ava.Services;

namespace MeineApps.Core.Premium.Ava.Droid;

/// <summary>
/// Android-Implementierung von IRewardedAdService mit echten AdMob Rewarded Ads.
/// Linked File - wird per Compile Include in Android-Projekte eingebunden.
/// Ersetzt den Desktop-RewardedAdService per DI Override in MainActivity.
/// Unterstuetzt Multi-Placement: verschiedene Ad-Unit-IDs pro Feature.
/// </summary>
public class AndroidRewardedAdService : IRewardedAdService
{
    private readonly RewardedAdHelper _helper;
    private readonly IPurchaseService _purchaseService;
    private readonly string _appName;
    private bool _isDisabled;

    /// <param name="helper">RewardedAdHelper Instanz (wird in MainActivity erstellt)</param>
    /// <param name="purchaseService">Fuer Premium-Check</param>
    /// <param name="appName">App-Name fuer AdConfig Lookup (z.B. "BomberBlast")</param>
    public AndroidRewardedAdService(RewardedAdHelper helper, IPurchaseService purchaseService, string appName)
    {
        _helper = helper;
        _purchaseService = purchaseService;
        _appName = appName;
    }

    public bool IsAvailable => !_isDisabled && !_purchaseService.IsPremium;

    public async Task<bool> ShowAdAsync()
    {
        if (!IsAvailable) return false;

        // Default-Placement: Vorgeladene Ad zeigen
        if (!_helper.IsLoaded)
        {
            await Task.Delay(2000);
            if (!_helper.IsLoaded) return false;
        }

        return await _helper.ShowAsync();
    }

    public async Task<bool> ShowAdAsync(string placement)
    {
        if (!IsAvailable) return false;

        // Placement-spezifische Ad-Unit-ID aus AdConfig holen
        var adUnitId = AdConfig.GetRewardedAdUnitId(_appName, placement);

        // On-Demand laden und zeigen (eigene Ad-Unit-ID pro Placement)
        return await _helper.LoadAndShowAsync(adUnitId);
    }

    public void Disable()
    {
        _isDisabled = true;
    }
}
