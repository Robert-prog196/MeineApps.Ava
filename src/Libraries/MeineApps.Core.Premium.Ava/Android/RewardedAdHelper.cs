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
    private const string Tag = "RewardedAdHelper";
    private const int LoadTimeoutMs = 8000; // 8 Sekunden Timeout fuer On-Demand Ad-Laden

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
        Android.Util.Log.Info(Tag, $"Load aufgerufen mit AdUnitId: {adUnitId}");
        LoadInternal(adUnitId);
    }

    private void LoadInternal(string adUnitId)
    {
        if (_isLoading || _activity == null || _disposed)
        {
            Android.Util.Log.Warn(Tag, $"LoadInternal abgebrochen: isLoading={_isLoading}, activity={_activity != null}, disposed={_disposed}");
            return;
        }
        _isLoading = true;

        try
        {
            var adRequest = new AdRequest.Builder().Build();
            Android.Util.Log.Info(Tag, $"Lade Rewarded Ad: {adUnitId}");
            _activity.RunOnUiThread(() =>
            {
                RewardedAd.Load(_activity, adUnitId, adRequest, new LoadCallback(this));
            });
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error(Tag, $"LoadInternal Exception: {ex.Message}");
            _isLoading = false;
        }
    }

    /// <summary>Zeigt die vorgeladene Rewarded Ad. Gibt true zurueck wenn User Belohnung verdient hat.</summary>
    public Task<bool> ShowAsync()
    {
        if (_rewardedAd == null || _activity == null)
        {
            Android.Util.Log.Warn(Tag, $"ShowAsync abgebrochen: rewardedAd={_rewardedAd != null}, activity={_activity != null}");
            return Task.FromResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        var fullScreenCallback = new FullScreenCallback(tcs, this);
        var rewardCallback = new RewardCallback(fullScreenCallback);

        Android.Util.Log.Info(Tag, "ShowAsync: Zeige vorgeladene Rewarded Ad");
        _activity.RunOnUiThread(() =>
        {
            try
            {
                _rewardedAd!.FullScreenContentCallback = fullScreenCallback;
                _rewardedAd.Show(_activity!, rewardCallback);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(Tag, $"ShowAsync Exception: {ex.Message}");
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
    public async Task<bool> LoadAndShowAsync(string adUnitId)
    {
        if (_activity == null || _disposed)
        {
            Android.Util.Log.Warn(Tag, $"LoadAndShowAsync abgebrochen: activity={_activity != null}, disposed={_disposed}");
            return false;
        }

        var tcs = new TaskCompletionSource<bool>();
        var activity = _activity;

        try
        {
            var adRequest = new AdRequest.Builder().Build();
            Android.Util.Log.Info(Tag, $"LoadAndShowAsync: Lade Ad on-demand: {adUnitId}");
            activity.RunOnUiThread(() =>
            {
                RewardedAd.Load(activity, adUnitId, adRequest, new OnDemandLoadCallback(this, activity, tcs));
            });
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error(Tag, $"LoadAndShowAsync Exception: {ex.Message}");
            tcs.TrySetResult(false);
        }

        // Timeout damit der await nicht ewig haengt falls Callback nie feuert
        var timeoutTask = Task.Delay(LoadTimeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Android.Util.Log.Error(Tag, $"LoadAndShowAsync TIMEOUT nach {LoadTimeoutMs}ms fuer: {adUnitId}");
            tcs.TrySetResult(false);
            return false;
        }

        return await tcs.Task;
    }

    private void OnAdLoaded(RewardedAd ad)
    {
        Android.Util.Log.Info(Tag, "Pre-Load: Rewarded Ad erfolgreich geladen");
        _rewardedAd = ad;
        _isLoading = false;
    }

    private void OnAdFailedToLoad(LoadAdError error)
    {
        Android.Util.Log.Error(Tag, $"Pre-Load FEHLGESCHLAGEN: Code={error.Code}, Message={error.Message}, Domain={error.Domain}");
        _rewardedAd = null;
        _isLoading = false;
    }

    private void OnAdDismissed()
    {
        // Nach dem Schliessen neue Default-Ad laden fuer naechste Nutzung
        Android.Util.Log.Info(Tag, "Ad geschlossen, lade naechste Default-Ad");
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

        public override void OnAdFailedToLoad(LoadAdError error) => _helper.OnAdFailedToLoad(error);
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
            Android.Util.Log.Info(Tag, "On-Demand: Rewarded Ad geladen, zeige sofort");
            // Ad geladen → sofort zeigen
            var fullScreenCallback = new FullScreenCallback(_tcs, null);
            var rewardCallback = new RewardCallback(fullScreenCallback);
            _activity.RunOnUiThread(() =>
            {
                try
                {
                    ad.FullScreenContentCallback = fullScreenCallback;
                    ad.Show(_activity, rewardCallback);
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error(Tag, $"On-Demand Show Exception: {ex.Message}");
                    _tcs.TrySetResult(false);
                }
            });
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            Android.Util.Log.Error(Tag, $"On-Demand Load FEHLGESCHLAGEN: Code={error.Code}, Message={error.Message}, Domain={error.Domain}");
            _tcs.TrySetResult(false);
        }
    }

    /// <summary>
    /// Separater Callback fuer FullScreenContent (Ad-Anzeige-Lifecycle).
    /// GETRENNT von IOnUserEarnedRewardListener um ACW-Probleme bei Dual-Inheritance zu vermeiden.
    /// </summary>
    private sealed class FullScreenCallback : FullScreenContentCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private readonly RewardedAdHelper? _helper;
        internal bool Rewarded;

        public FullScreenCallback(TaskCompletionSource<bool> tcs, RewardedAdHelper? helper)
        {
            _tcs = tcs;
            _helper = helper;
        }

        public override void OnAdShowedFullScreenContent()
        {
            Android.Util.Log.Info(Tag, "Rewarded Ad wird angezeigt (Fullscreen)");
        }

        public override void OnAdDismissedFullScreenContent()
        {
            Android.Util.Log.Info(Tag, $"Rewarded Ad geschlossen, Belohnung verdient: {Rewarded}");
            _tcs.TrySetResult(Rewarded);
            _helper?.OnAdDismissed();
        }

        public override void OnAdFailedToShowFullScreenContent(AdError error)
        {
            Android.Util.Log.Error(Tag, $"Rewarded Ad Show FEHLGESCHLAGEN: Code={error.Code}, Message={error.Message}, Domain={error.Domain}");
            _tcs.TrySetResult(false);
        }
    }

    /// <summary>
    /// Separater Callback fuer Belohnung (IOnUserEarnedRewardListener).
    /// Eigene Klasse statt Dual-Inheritance auf FullScreenContentCallback.
    /// </summary>
    private sealed class RewardCallback : Java.Lang.Object, IOnUserEarnedRewardListener
    {
        private readonly FullScreenCallback _fullScreenCallback;

        public RewardCallback(FullScreenCallback fullScreenCallback) => _fullScreenCallback = fullScreenCallback;

        public void OnUserEarnedReward(IRewardItem reward)
        {
            Android.Util.Log.Info(Tag, $"Belohnung verdient: Type={reward.Type}, Amount={reward.Amount}");
            _fullScreenCallback.Rewarded = true;
        }
    }
}
