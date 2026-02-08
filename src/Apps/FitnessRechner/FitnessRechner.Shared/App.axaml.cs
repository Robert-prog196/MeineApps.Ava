using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Extensions;
using MeineApps.Core.Premium.Ava.Services;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;
using FitnessRechner.Services;
using FitnessRechner.ViewModels;
using FitnessRechner.ViewModels.Calculators;
using FitnessRechner.Views;

namespace FitnessRechner;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Factory fuer plattformspezifischen IRewardedAdService (Android setzt RewardedAdHelper).
    /// Nimmt IServiceProvider entgegen fuer Lazy-Resolution von Abhaengigkeiten.
    /// </summary>
    public static Func<IServiceProvider, IRewardedAdService>? RewardedAdServiceFactory { get; set; }

    /// <summary>
    /// Factory fuer plattformspezifischen IFileShareService.
    /// Android setzt dies auf AndroidFileShareService.
    /// </summary>
    public static Func<IFileShareService>? FileShareServiceFactory { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
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
                try
                {
                    // Debug: Views einzeln testen um Crash-Verursacher zu finden
                    var errors = new System.Text.StringBuilder();
                    errors.AppendLine("=== VIEW CONSTRUCTION TEST ===\n");

                    // Test 1: Leeres MainView (ohne DataContext)
                    try
                    {
                        var mv = new MainView();
                        errors.AppendLine("PASS: new MainView()");
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: new MainView()\n{ex.GetType().Name}: {ex.Message}\n");
                    }

                    // Test 2: HomeView
                    try
                    {
                        var hv = new Views.HomeView();
                        errors.AppendLine("PASS: new HomeView()");
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: new HomeView()\n{ex.GetType().Name}: {ex.Message}\n");
                    }

                    // Test 3: ProgressView
                    try
                    {
                        var pv = new Views.ProgressView();
                        errors.AppendLine("PASS: new ProgressView()");
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: new ProgressView()\n{ex.GetType().Name}: {ex.Message}\n");
                    }

                    // Test 4: FoodSearchView
                    try
                    {
                        var fv = new Views.FoodSearchView();
                        errors.AppendLine("PASS: new FoodSearchView()");
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: new FoodSearchView()\n{ex.GetType().Name}: {ex.Message}\n");
                    }

                    // Test 5: SettingsView
                    try
                    {
                        var sv = new Views.SettingsView();
                        errors.AppendLine("PASS: new SettingsView()");
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: new SettingsView()\n{ex.GetType().Name}: {ex.Message}\n");
                    }

                    // Test 6: MainView + DataContext + Attach
                    var mainViewSuccess = false;
                    try
                    {
                        var vm = Services.GetRequiredService<MainViewModel>();
                        errors.AppendLine("PASS: MainViewModel resolved");

                        var mainView = new MainView();
                        mainView.DataContext = vm;
                        errors.AppendLine("PASS: MainView.DataContext set");

                        singleViewPlatform.MainView = mainView;
                        errors.AppendLine("PASS: singleViewPlatform.MainView set");
                        mainViewSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"FAIL: MainView attach\n{ex}\n");
                    }

                    // Falls Fehler: Ergebnis anzeigen
                    if (!mainViewSuccess)
                    {
                        singleViewPlatform.MainView = new ScrollViewer
                        {
                            Content = new TextBlock
                            {
                                Text = errors.ToString(),
                                Foreground = Brushes.Red,
                                FontSize = 11,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Avalonia.Thickness(12, 40, 12, 16)
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    singleViewPlatform.MainView = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = $"STARTUP ERROR:\n\n{ex}",
                            Foreground = Brushes.Red,
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(16, 40, 16, 16)
                        }
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            // Auf Android: Fehler im UI anzeigen wenn moeglich
            if (ApplicationLifetime is ISingleViewApplicationLifetime svp)
            {
                svp.MainView = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = $"FATAL INIT ERROR:\n\n{ex}",
                        Foreground = Brushes.Red,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(16, 40, 16, 16)
                    }
                };
                base.OnFrameworkInitializationCompleted();
                return;
            }
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IPreferencesService>(sp => new PreferencesService("FitnessRechner"));
        services.AddSingleton<IThemeService, ThemeService>();

        // Premium Services (Ads, Purchases)
        services.AddMeineAppsPremium();

        // Android-Override: Echte Rewarded Ads statt Desktop-Simulator
        if (RewardedAdServiceFactory != null)
            services.AddSingleton<IRewardedAdService>(sp => RewardedAdServiceFactory!(sp));

        services.AddSingleton<IScanLimitService, ScanLimitService>();

        // File Share Service (Desktop: Datei oeffnen, Android: Share Intent)
        if (FileShareServiceFactory != null)
            services.AddSingleton<IFileShareService>(_ => FileShareServiceFactory!());
        else
            services.AddSingleton<IFileShareService, DesktopFileShareService>();

        // Localization
        services.AddSingleton<ILocalizationService>(sp =>
            new LocalizationService(AppStrings.ResourceManager, sp.GetRequiredService<IPreferencesService>()));

        // App Services
        services.AddSingleton<FitnessEngine>();
        services.AddSingleton<ITrackingService, TrackingService>();
        services.AddSingleton<IFoodSearchService, FoodSearchService>();
        services.AddSingleton<IBarcodeLookupService, BarcodeLookupService>();
        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<FoodSearchViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TrackingViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<BarcodeScannerViewModel>();

        // Calculator ViewModels
        services.AddTransient<BmiViewModel>();
        services.AddTransient<CaloriesViewModel>();
        services.AddTransient<WaterViewModel>();
        services.AddTransient<IdealWeightViewModel>();
        services.AddTransient<BodyFatViewModel>();
    }
}
