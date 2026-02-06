using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using MeineApps.Core.Premium.Ava.Droid;
using MeineApps.Core.Premium.Ava.Services;

namespace HandwerkerImperium;

[Activity(
    Label = "HandwerkerImperium",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/appicon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private AdMobHelper? _adMobHelper;

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Google Mobile Ads + GDPR consent
        AdMobHelper.Initialize(this);
        AdMobHelper.RequestConsent(this);

        // Banner ad above the tab bar (64dp: 48dp buttons + 16dp padding)
        _adMobHelper = new AdMobHelper();
        var adService = App.Services.GetRequiredService<IAdService>();
        var purchaseService = App.Services.GetRequiredService<IPurchaseService>();
        _adMobHelper.AttachToActivity(this, AdConfig.GetBannerAdUnitId("HandwerkerImperium"), adService, purchaseService, 64);
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
        _adMobHelper?.Dispose();
        base.OnDestroy();
    }
}
