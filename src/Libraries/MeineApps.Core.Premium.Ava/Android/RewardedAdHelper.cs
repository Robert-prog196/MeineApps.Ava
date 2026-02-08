using Android.App;
using Android.Gms.Ads;
using Android.Gms.Ads.Rewarded;

namespace MeineApps.Core.Premium.Ava.Droid;

/// <summary>
/// Helper fuer Google AdMob Rewarded Ads auf Android.
/// Linked File - wird per Compile Include in Android-Projekte eingebunden.
/// NICHT kompiliert im net10.0 Library-Projekt.
/// Unterstuetzt Pre-Loading (Load + ShowAsync) und On-Demand (LoadAndShowAsync).
/// </summary>
public sealed class RewardedAdHelper : IDisposable
{
    private RewardedAd? _rewardedAd;
    private Activity? _activity;
    private string _defaultAdUnitId = "";
    private bool _isLoading;
    private bool _disposed;

    /// <summary>Ob eine Rewarded Ad geladen und bereit ist</summary>
    public bool IsLoaded => _rewardedAd != null;

    /// <summary>Initialisiert und laedt die erste Rewarded Ad (Default-Placement)</summary>
    public void Load(Activity activity, string adUnitId)
    {
        _activity = activity;
        _defaultAdUnitId = adUnitId;
        LoadInternal(adUnitId);
    }

    private void LoadInternal(string adUnitId)
    {
        if (_isLoading || _activity == null || _disposed) return;
        _isLoading = true;

        try
        {
            var adRequest = new AdRequest.Builder().Build();
            _activity.RunOnUiThread(() =>
            {
                RewardedAd.Load(_activity, adUnitId, adRequest, new LoadCallback(this));
            });
        }
        catch
        {
            _isLoading = false;
        }
    }

    /// <summary>Zeigt die vorgeladene Rewarded Ad. Gibt true zurueck wenn User Belohnung verdient hat.</summary>
    public Task<bool> ShowAsync()
    {
        if (_rewardedAd == null || _activity == null)
            return Task.FromResult(false);

        var tcs = new TaskCompletionSource<bool>();
        var callback = new ShowCallback(tcs);

        _activity.RunOnUiThread(() =>
        {
            try
            {
                _rewardedAd!.FullScreenContentCallback = callback;
                _rewardedAd.Show(_activity!, callback);
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Laedt eine Rewarded Ad mit einer bestimmten Ad-Unit-ID und zeigt sie sofort.
    /// Fuer Placements die nicht vorgeladen werden (On-Demand).
    /// Gibt true zurueck wenn User Belohnung verdient hat.
    /// </summary>
    public Task<bool> LoadAndShowAsync(string adUnitId)
    {
        if (_activity == null || _disposed)
            return Task.FromResult(false);

        var tcs = new TaskCompletionSource<bool>();
        var activity = _activity;

        try
        {
            var adRequest = new AdRequest.Builder().Build();
            activity.RunOnUiThread(() =>
            {
                RewardedAd.Load(activity, adUnitId, adRequest, new OnDemandLoadCallback(this, activity, tcs));
            });
        }
        catch
        {
            tcs.TrySetResult(false);
        }

        return tcs.Task;
    }

    private void OnAdLoaded(RewardedAd ad)
    {
        _rewardedAd = ad;
        _isLoading = false;
    }

    private void OnAdFailedToLoad()
    {
        _rewardedAd = null;
        _isLoading = false;
    }

    private void OnAdDismissed()
    {
        // Nach dem Schliessen neue Default-Ad laden fuer naechste Nutzung
        _rewardedAd = null;
        if (!string.IsNullOrEmpty(_defaultAdUnitId))
            LoadInternal(_defaultAdUnitId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rewardedAd = null;
        _activity = null;
    }

    /// <summary>Callback fuer Pre-Load Ad-Ladevorgang (Default-Placement)</summary>
    private sealed class LoadCallback : RewardedAdLoadCallback
    {
        private readonly RewardedAdHelper _helper;

        public LoadCallback(RewardedAdHelper helper) => _helper = helper;

        // Register mit Java-Signatur fuer RewardedAd (nicht Object) um Erasure-Konflikt zu vermeiden
        [Android.Runtime.Register("onAdLoaded", "(Lcom/google/android/gms/ads/rewarded/RewardedAd;)V", "")]
        public void OnAdLoaded(RewardedAd ad) => _helper.OnAdLoaded(ad);

        public override void OnAdFailedToLoad(LoadAdError error) => _helper.OnAdFailedToLoad();
    }

    /// <summary>Callback fuer On-Demand Load+Show (laedt Ad und zeigt sie sofort)</summary>
    private sealed class OnDemandLoadCallback : RewardedAdLoadCallback
    {
        private readonly RewardedAdHelper _helper;
        private readonly Activity _activity;
        private readonly TaskCompletionSource<bool> _tcs;

        public OnDemandLoadCallback(RewardedAdHelper helper, Activity activity, TaskCompletionSource<bool> tcs)
        {
            _helper = helper;
            _activity = activity;
            _tcs = tcs;
        }

        [Android.Runtime.Register("onAdLoaded", "(Lcom/google/android/gms/ads/rewarded/RewardedAd;)V", "")]
        public void OnAdLoaded(RewardedAd ad)
        {
            // Ad geladen → sofort zeigen
            var showCallback = new ShowCallback(_tcs);
            _activity.RunOnUiThread(() =>
            {
                try
                {
                    ad.FullScreenContentCallback = showCallback;
                    ad.Show(_activity, showCallback);
                }
                catch
                {
                    _tcs.TrySetResult(false);
                }
            });
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            _tcs.TrySetResult(false);
        }
    }

    /// <summary>Callback fuer Ad-Anzeige + Belohnung</summary>
    private sealed class ShowCallback : FullScreenContentCallback, IOnUserEarnedRewardListener
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private bool _rewarded;

        public ShowCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public void OnUserEarnedReward(IRewardItem reward)
        {
            _rewarded = true;
        }

        public override void OnAdDismissedFullScreenContent()
        {
            _tcs.TrySetResult(_rewarded);
        }

        public override void OnAdFailedToShowFullScreenContent(AdError error)
        {
            _tcs.TrySetResult(false);
        }
    }
}
