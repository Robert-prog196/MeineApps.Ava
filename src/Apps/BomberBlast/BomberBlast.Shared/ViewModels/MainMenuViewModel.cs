using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomberBlast.Services;
using MeineApps.Core.Premium.Ava.Services;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel for the main menu page.
/// Provides navigation commands for Story, Arcade, Quick Play, and other menu options.
/// </summary>
public partial class MainMenuViewModel : ObservableObject
{
    private readonly IProgressService _progressService;
    private readonly IPurchaseService _purchaseService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event to request navigation. Parameter is the route string.
    /// </summary>
    public event Action<string>? NavigationRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool _showContinueButton;

    [ObservableProperty]
    private string _versionText = "v1.0.0 - RS-Digital";

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether ads should be shown (not premium).
    /// </summary>
    public bool ShowAds => !_purchaseService.IsPremium;

    /// <summary>
    /// Whether the player has progress to continue (alias for ShowContinueButton).
    /// </summary>
    public bool HasProgress => ShowContinueButton;

    public MainMenuViewModel(IProgressService progressService, IPurchaseService purchaseService)
    {
        _progressService = progressService;
        _purchaseService = purchaseService;

        // Set version from assembly
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version;
        VersionText = version != null
            ? $"v{version.Major}.{version.Minor}.{version.Build} - RS-Digital"
            : "v1.0.0 - RS-Digital";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the view appears. Refreshes continue button visibility.
    /// </summary>
    public void OnAppearing()
    {
        ShowContinueButton = _progressService.HighestCompletedLevel > 0;
        OnPropertyChanged(nameof(HasProgress));
        OnPropertyChanged(nameof(ShowAds));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void StoryMode()
    {
        NavigationRequested?.Invoke("LevelSelect");
    }

    [RelayCommand]
    private void Continue()
    {
        int nextLevel = Math.Min(
            _progressService.HighestCompletedLevel + 1,
            _progressService.TotalLevels);
        NavigationRequested?.Invoke($"Game?mode=story&level={nextLevel}");
    }

    [RelayCommand]
    private void ArcadeMode()
    {
        NavigationRequested?.Invoke("Game?mode=arcade");
    }

    [RelayCommand]
    private void QuickPlay()
    {
        NavigationRequested?.Invoke("Game?mode=quick");
    }

    [RelayCommand]
    private void HighScores()
    {
        NavigationRequested?.Invoke("HighScores");
    }

    [RelayCommand]
    private void Help()
    {
        NavigationRequested?.Invoke("Help");
    }

    [RelayCommand]
    private void Settings()
    {
        NavigationRequested?.Invoke("Settings");
    }
}
