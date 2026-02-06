using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Premium.Ava.Services;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel for the game over page.
/// Displays the final score, level reached, and high score status.
/// </summary>
public partial class GameOverViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly ILocalizationService _localizationService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event to request navigation. Parameter is the route string.
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Whether ads should be shown (not premium).
    /// </summary>
    public bool ShowAds => !_purchaseService.IsPremium;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private int _score;

    [ObservableProperty]
    private string _scoreText = "";

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private string _levelText = "";

    [ObservableProperty]
    private bool _isHighScore;

    [ObservableProperty]
    private string _mode = "story";

    [ObservableProperty]
    private bool _isArcadeMode;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public GameOverViewModel(IPurchaseService purchaseService, ILocalizationService localizationService)
    {
        _purchaseService = purchaseService;
        _localizationService = localizationService;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set the game over parameters from navigation query.
    /// </summary>
    public void SetParameters(int score, int level, bool isHighScore, string mode)
    {
        Score = score;
        Level = level;
        IsHighScore = isHighScore;
        Mode = mode;
        IsArcadeMode = mode == "arcade";

        ScoreText = score.ToString("N0");
        LevelText = IsArcadeMode
            ? string.Format(_localizationService.GetString("WaveOverlay"), level)
            : string.Format(_localizationService.GetString("LevelFormat"), level);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void TryAgain()
    {
        if (IsArcadeMode)
        {
            NavigationRequested?.Invoke("//MainMenu/Game?mode=arcade");
        }
        else
        {
            NavigationRequested?.Invoke($"//MainMenu/Game?mode=story&level={Level}");
        }
    }

    [RelayCommand]
    private void MainMenu()
    {
        NavigationRequested?.Invoke("//MainMenu");
    }
}
