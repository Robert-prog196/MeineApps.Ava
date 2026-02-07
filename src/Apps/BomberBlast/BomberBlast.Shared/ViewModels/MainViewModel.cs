using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;

namespace BomberBlast.ViewModels;

/// <summary>
/// Main ViewModel that manages navigation between views (MainMenu, Game, LevelSelect, etc.).
/// Acts as a view-switcher: only one child view is visible at a time.
/// Holds child ViewModels so each view gets the correct DataContext.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ═══════════════════════════════════════════════════════════════════════
    // CHILD VIEWMODELS
    // ═══════════════════════════════════════════════════════════════════════

    public MainMenuViewModel MenuVm { get; }
    public GameViewModel GameVm { get; }
    public LevelSelectViewModel LevelSelectVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public HighScoresViewModel HighScoresVm { get; }
    public GameOverViewModel GameOverVm { get; }
    public PauseViewModel PauseVm { get; }
    public HelpViewModel HelpVm { get; }

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isMainMenuActive = true;

    [ObservableProperty]
    private bool _isGameActive;

    [ObservableProperty]
    private bool _isLevelSelectActive;

    [ObservableProperty]
    private bool _isSettingsActive;

    [ObservableProperty]
    private bool _isHighScoresActive;

    [ObservableProperty]
    private bool _isGameOverActive;

    [ObservableProperty]
    private bool _isHelpActive;

    // ═══════════════════════════════════════════════════════════════════════
    // DIALOG PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isAlertDialogVisible;

    [ObservableProperty]
    private string _alertDialogTitle = "";

    [ObservableProperty]
    private string _alertDialogMessage = "";

    [ObservableProperty]
    private string _alertDialogButtonText = "";

    [ObservableProperty]
    private bool _isConfirmDialogVisible;

    [ObservableProperty]
    private string _confirmDialogTitle = "";

    [ObservableProperty]
    private string _confirmDialogMessage = "";

    [ObservableProperty]
    private string _confirmDialogAcceptText = "";

    [ObservableProperty]
    private string _confirmDialogCancelText = "";

    private TaskCompletionSource<bool>? _confirmDialogTcs;

    /// <summary>
    /// Tracks whether Settings was opened from within a game (for back-navigation).
    /// </summary>
    private bool _returnToGameFromSettings;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public MainViewModel(
        MainMenuViewModel menuVm,
        GameViewModel gameVm,
        LevelSelectViewModel levelSelectVm,
        SettingsViewModel settingsVm,
        HighScoresViewModel highScoresVm,
        GameOverViewModel gameOverVm,
        PauseViewModel pauseVm,
        HelpViewModel helpVm,
        ILocalizationService localization)
    {
        MenuVm = menuVm;
        GameVm = gameVm;
        LevelSelectVm = levelSelectVm;
        SettingsVm = settingsVm;
        HighScoresVm = highScoresVm;
        GameOverVm = gameOverVm;
        PauseVm = pauseVm;
        HelpVm = helpVm;

        // Wire up navigation from child VMs
        WireNavigation(menuVm);
        WireNavigation(gameVm);
        WireNavigation(levelSelectVm);
        WireNavigation(settingsVm);
        WireNavigation(highScoresVm);
        WireNavigation(gameOverVm);
        WireNavigation(pauseVm);
        WireNavigation(helpVm);

        // Wire up dialog events from SettingsVM
        settingsVm.AlertRequested += (t, m, b) => ShowAlertDialog(t, m, b);
        settingsVm.ConfirmationRequested += (t, m, a, c) => ShowConfirmDialog(t, m, a, c);

        localization.LanguageChanged += (_, _) =>
        {
            // Child VMs re-read their localized texts on next OnAppearing
            MenuVm.OnAppearing();
        };

        // Initialize menu
        menuVm.OnAppearing();
    }

    private void WireNavigation(ObservableObject vm)
    {
        var navEvent = vm.GetType().GetEvent("NavigationRequested");
        if (navEvent != null)
        {
            navEvent.AddEventHandler(vm, new Action<string>(route => NavigateTo(route)));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigate to a specific view. Hides all others.
    /// Supports routes like "Game?mode=story&amp;level=5" and
    /// compound routes like "//MainMenu/Game?mode=arcade".
    /// </summary>
    public async void NavigateTo(string route)
    {
        // Handle compound routes (e.g., "//MainMenu/Game?mode=arcade")
        if (route.StartsWith("//"))
        {
            var withoutPrefix = route[2..];
            var slashIndex = withoutPrefix.IndexOf('/');
            if (slashIndex >= 0)
                route = withoutPrefix[(slashIndex + 1)..];
            else
                route = withoutPrefix;
        }

        var baseRoute = route.Contains('?') ? route[..route.IndexOf('?')] : route;

        // Save current state before hiding (needed for back-navigation)
        var wasGameActive = IsGameActive;

        // Lifecycle: stop game loop when leaving Game view
        if (wasGameActive && baseRoute != "Game")
        {
            GameVm.OnDisappearing();
        }

        HideAll();

        switch (baseRoute)
        {
            case "MainMenu":
                _returnToGameFromSettings = false;
                IsMainMenuActive = true;
                MenuVm.OnAppearing();
                break;

            case "Game":
                IsGameActive = true;
                // Parse game parameters
                if (route.Contains('?'))
                {
                    var query = route[(route.IndexOf('?') + 1)..];
                    var mode = "quick";
                    var level = 1;
                    foreach (var param in query.Split('&'))
                    {
                        var parts = param.Split('=');
                        if (parts.Length == 2)
                        {
                            if (parts[0] == "mode") mode = parts[1];
                            if (parts[0] == "level") int.TryParse(parts[1], out level);
                        }
                    }
                    GameVm.SetParameters(mode, level);
                }
                // Start the game (initializes engine + starts 60fps loop)
                await GameVm.OnAppearingAsync();
                break;

            case "LevelSelect":
                IsLevelSelectActive = true;
                LevelSelectVm.OnAppearing();
                break;

            case "Settings":
                _returnToGameFromSettings = wasGameActive;
                IsSettingsActive = true;
                SettingsVm.OnAppearing();
                break;

            case "HighScores":
                IsHighScoresActive = true;
                HighScoresVm.OnAppearing();
                break;

            case "GameOver":
                IsGameOverActive = true;
                // Parse game over parameters and pass to GameOverVm
                if (route.Contains('?'))
                {
                    var query = route[(route.IndexOf('?') + 1)..];
                    var score = 0;
                    var level = 0;
                    var isHighScore = false;
                    var mode = "story";
                    foreach (var param in query.Split('&'))
                    {
                        var parts = param.Split('=');
                        if (parts.Length == 2)
                        {
                            switch (parts[0])
                            {
                                case "score": int.TryParse(parts[1], out score); break;
                                case "level": int.TryParse(parts[1], out level); break;
                                case "highscore": bool.TryParse(parts[1], out isHighScore); break;
                                case "mode": mode = parts[1]; break;
                            }
                        }
                    }
                    GameOverVm.SetParameters(score, level, isHighScore, mode);
                }
                break;

            case "Help":
                IsHelpActive = true;
                break;

            case "..":
                // Back-navigation: return to Game if Settings was opened from Game
                if (_returnToGameFromSettings)
                {
                    _returnToGameFromSettings = false;
                    IsGameActive = true;
                    await GameVm.OnAppearingAsync();
                }
                else
                {
                    IsMainMenuActive = true;
                    MenuVm.OnAppearing();
                }
                break;

            default:
                IsMainMenuActive = true;
                MenuVm.OnAppearing();
                break;
        }
    }

    private void HideAll()
    {
        IsMainMenuActive = false;
        IsGameActive = false;
        IsLevelSelectActive = false;
        IsSettingsActive = false;
        IsHighScoresActive = false;
        IsGameOverActive = false;
        IsHelpActive = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DIALOGS
    // ═══════════════════════════════════════════════════════════════════════

    private void ShowAlertDialog(string title, string message, string buttonText)
    {
        AlertDialogTitle = title;
        AlertDialogMessage = message;
        AlertDialogButtonText = buttonText;
        IsAlertDialogVisible = true;
    }

    [RelayCommand]
    private void DismissAlert()
    {
        IsAlertDialogVisible = false;
    }

    private Task<bool> ShowConfirmDialog(string title, string message, string acceptText, string cancelText)
    {
        ConfirmDialogTitle = title;
        ConfirmDialogMessage = message;
        ConfirmDialogAcceptText = acceptText;
        ConfirmDialogCancelText = cancelText;
        _confirmDialogTcs = new TaskCompletionSource<bool>();
        IsConfirmDialogVisible = true;
        return _confirmDialogTcs.Task;
    }

    [RelayCommand]
    private void AcceptConfirm()
    {
        IsConfirmDialogVisible = false;
        _confirmDialogTcs?.TrySetResult(true);
    }

    [RelayCommand]
    private void CancelConfirm()
    {
        IsConfirmDialogVisible = false;
        _confirmDialogTcs?.TrySetResult(false);
    }
}
