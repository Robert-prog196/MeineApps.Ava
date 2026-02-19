using BomberBlast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel fuer den Victory-Screen (alle 50 Level geschafft).
/// Zeigt Glueckwunsch, Sterne-Zaehler und Dankes-Text.
/// </summary>
public partial class VictoryViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly IProgressService _progressService;

    public event Action<string>? NavigationRequested;

    [ObservableProperty]
    private string _titleText = "";

    [ObservableProperty]
    private string _subtitleText = "";

    [ObservableProperty]
    private string _starsText = "";

    [ObservableProperty]
    private string _thanksText = "";

    [ObservableProperty]
    private int _totalStars;

    [ObservableProperty]
    private int _scoreTotal;

    [ObservableProperty]
    private string _scoreTotalText = "";

    public VictoryViewModel(ILocalizationService localizationService, IProgressService progressService)
    {
        _localizationService = localizationService;
        _progressService = progressService;
    }

    public void OnAppearing()
    {
        TitleText = _localizationService.GetString("VictoryTitle") ?? "Victory!";
        SubtitleText = _localizationService.GetString("VictorySubtitle") ?? "You completed all 50 levels!";
        ThanksText = _localizationService.GetString("VictoryThanks") ?? "Thank you for playing!";

        TotalStars = _progressService.GetTotalStars();
        StarsText = string.Format(
            _localizationService.GetString("VictoryStars") ?? "Total Stars: {0}/150",
            TotalStars);
    }

    /// <summary>
    /// Setzt den Gesamtscore fuer die Anzeige (aus GameOver-Daten).
    /// </summary>
    public void SetScore(int score)
    {
        ScoreTotal = score;
        ScoreTotalText = score.ToString("N0");
    }

    [RelayCommand]
    private void GoToMainMenu() => NavigationRequested?.Invoke("//MainMenu");

    [RelayCommand]
    private void GoToShop() => NavigationRequested?.Invoke("Shop");
}
