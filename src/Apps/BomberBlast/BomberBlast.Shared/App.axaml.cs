using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Extensions;
using MeineApps.Core.Premium.Ava.Services;
using BomberBlast.Core;
using BomberBlast.Graphics;
using BomberBlast.Input;
using BomberBlast.Resources.Strings;
using BomberBlast.Services;
using BomberBlast.ViewModels;
using BomberBlast.Views;

namespace BomberBlast;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize localization
        var locService = Services.GetRequiredService<ILocalizationService>();
        locService.Initialize();
        LocalizationManager.Initialize(locService);

        // Initialize theme (must be resolved to apply saved theme at startup)
        _ = Services.GetRequiredService<IThemeService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IPreferencesService>(sp => new PreferencesService("BomberBlast"));
        services.AddSingleton<IThemeService, ThemeService>();

        // Premium Services (Ads, Purchases)
        services.AddMeineAppsPremium();

        // Localization
        services.AddSingleton<ILocalizationService>(sp =>
            new LocalizationService(AppStrings.ResourceManager, sp.GetRequiredService<IPreferencesService>()));

        // Game Services
        services.AddSingleton<IProgressService, ProgressService>();
        services.AddSingleton<IHighScoreService, HighScoreService>();
        services.AddSingleton<ISoundService, NullSoundService>();
        services.AddSingleton<SoundManager>();
        services.AddSingleton<SpriteSheet>();
        services.AddSingleton<InputManager>();
        services.AddSingleton<GameRenderer>();
        services.AddSingleton<GameEngine>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<GameViewModel>();
        services.AddTransient<LevelSelectViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HighScoresViewModel>();
        services.AddTransient<GameOverViewModel>();
        services.AddTransient<PauseViewModel>();
        services.AddTransient<HelpViewModel>();
    }
}
