using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using MeineApps.Core.Premium.Ava.Droid;
using MeineApps.Core.Premium.Ava.Services;

namespace BomberBlast;

[Activity(
    Label = "BomberBlast",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/appicon",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private AdMobHelper? _adMobHelper;
    private RewardedAdHelper? _rewardedAdHelper;

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Rewarded Ad Helper + Factory MUSS vor base.OnCreate (DI) registriert werden
        _rewardedAdHelper = new RewardedAdHelper();
        App.RewardedAdServiceFactory = sp =>
            new MeineApps.Core.Premium.Ava.Droid.AndroidRewardedAdService(
                _rewardedAdHelper!, sp.GetRequiredService<IPurchaseService>(), "BomberBlast");

        base.OnCreate(savedInstanceState);

        // Google Mobile Ads + GDPR consent
        AdMobHelper.Initialize(this);
        AdMobHelper.RequestConsent(this);

        // Banner ad at the bottom (no tab bar)
        _adMobHelper = new AdMobHelper();
        var adService = App.Services.GetRequiredService<IAdService>();
        var purchaseService = App.Services.GetRequiredService<IPurchaseService>();
        _adMobHelper.AttachToActivity(this, AdConfig.GetBannerAdUnitId("BomberBlast"), adService, purchaseService);

        // Rewarded Ad laden (nach DI-Build)
        _rewardedAdHelper.Load(this, AdConfig.GetRewardedAdUnitId("BomberBlast"));
    }

    protected override void OnResume()
    {
        base.OnResume();
        _adMobHelper?.Resume();
    }

    protected override void OnPause()
    {
        _adMobHelper?.Pause();
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        _rewardedAdHelper?.Dispose();
        _adMobHelper?.Dispose();
        base.OnDestroy();
    }
}
