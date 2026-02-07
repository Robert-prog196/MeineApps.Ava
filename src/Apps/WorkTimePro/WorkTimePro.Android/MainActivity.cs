using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Droid;
using MeineApps.Core.Premium.Ava.Services;

namespace WorkTimePro.Android;

[Activity(
    Label = "WorkTimePro",
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
        // FileShareService MUSS vor base.OnCreate (DI) registriert werden
        App.FileShareServiceFactory = () => new AndroidFileShareService(this);

        base.OnCreate(savedInstanceState);

        // Google Mobile Ads + GDPR consent
        AdMobHelper.Initialize(this);
        AdMobHelper.RequestConsent(this);

        // Banner ad above the tab bar (56dp tab bar height)
        _adMobHelper = new AdMobHelper();
        var adService = App.Services.GetRequiredService<IAdService>();
        var purchaseService = App.Services.GetRequiredService<IPurchaseService>();
        _adMobHelper.AttachToActivity(this, AdConfig.GetBannerAdUnitId("WorkTimePro"), adService, purchaseService, 56);
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
