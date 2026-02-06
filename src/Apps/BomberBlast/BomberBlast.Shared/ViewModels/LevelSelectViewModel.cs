using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomberBlast.Services;
using MeineApps.Core.Premium.Ava.Services;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel for the level select page.
/// Displays a grid of 50 levels with completion status and stars.
/// </summary>
public partial class LevelSelectViewModel : ObservableObject
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
    private ObservableCollection<LevelDisplayItem> _levels = [];

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _starsText = "";

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Indicates whether ads should be shown (not premium).
    /// </summary>
    public bool ShowAds => !_purchaseService.IsPremium;

    public LevelSelectViewModel(IProgressService progressService, IPurchaseService purchaseService)
    {
        _progressService = progressService;
        _purchaseService = purchaseService;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the view appears. Builds the level grid and updates progress info.
    /// </summary>
    public void OnAppearing()
    {
        BuildLevelList();
        UpdateProgressInfo();
    }

    private void BuildLevelList()
    {
        Levels.Clear();

        for (int i = 1; i <= _progressService.TotalLevels; i++)
        {
            bool isUnlocked = _progressService.IsLevelUnlocked(i);
            int stars = _progressService.GetLevelStars(i);
            int bestScore = _progressService.GetLevelBestScore(i);
            bool isCompleted = bestScore > 0;

            var item = new LevelDisplayItem
            {
                LevelNumber = i,
                DisplayText = i.ToString(),
                IsUnlocked = isUnlocked,
                IsCompleted = isCompleted,
                Stars = stars,
                StarsText = isCompleted && stars > 0
                    ? new string('\u2605', stars) + new string('\u2606', 3 - stars)
                    : "",
                BestScore = bestScore
            };
            item.SelectCommand = new RelayCommand(() => SelectLevel(item));
            Levels.Add(item);
        }
    }

    private void UpdateProgressInfo()
    {
        int completed = _progressService.HighestCompletedLevel;
        int total = _progressService.TotalLevels;
        int stars = _progressService.GetTotalStars();
        int maxStars = total * 3;

        ProgressText = $"{completed}/{total}";
        StarsText = $"{stars}/{maxStars}";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void SelectLevel(LevelDisplayItem? level)
    {
        if (level == null || !level.IsUnlocked)
            return;

        NavigationRequested?.Invoke($"Game?mode=story&level={level.LevelNumber}");
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }
}

/// <summary>
/// Display model for a single level in the level select grid.
/// Also aliased as LevelItem for View DataTemplate compatibility.
/// </summary>
public class LevelDisplayItem
{
    public int LevelNumber { get; set; }
    public string DisplayText { get; set; } = "";
    public bool IsUnlocked { get; set; }
    public bool IsCompleted { get; set; }
    public int Stars { get; set; }
    public string StarsText { get; set; } = "";
    public int BestScore { get; set; }

    /// <summary>Command to select this level (bound from the ItemTemplate).</summary>
    public IRelayCommand? SelectCommand { get; set; }

    /// <summary>Whether this level is locked (inverse of IsUnlocked).</summary>
    public bool IsLocked => !IsUnlocked;

    /// <summary>Star display text for the view.</summary>
    public string StarsDisplay => StarsText;

    /// <summary>Background color based on level state.</summary>
    public Color BackgroundColor =>
        !IsUnlocked ? Color.Parse("#444444") :
        IsCompleted ? Color.Parse("#2E7D32") :
        Color.Parse("#1565C0");

    /// <summary>Text brush based on level state.</summary>
    public IBrush TextBrush =>
        !IsUnlocked ? Brushes.Gray : Brushes.White;
}

/// <summary>
/// Alias for LevelDisplayItem used in View DataTemplates.
/// </summary>
public class LevelItem : LevelDisplayItem { }
