using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.Services.Interfaces;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerImperium.ViewModels;

/// <summary>
/// ViewModel for the achievements page.
/// </summary>
public partial class AchievementsViewModel : ObservableObject
{
    private readonly IAchievementService _achievementService;
    private readonly ILocalizationService _localizationService;
    private readonly IAudioService _audioService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public event Action<string>? NavigationRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<AchievementDisplayModel> _achievements = [];

    [ObservableProperty]
    private int _unlockedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private AchievementCategory _selectedCategory = AchievementCategory.Orders;

    public AchievementsViewModel(
        IAchievementService achievementService,
        ILocalizationService localizationService,
        IAudioService audioService)
    {
        _achievementService = achievementService;
        _localizationService = localizationService;
        _audioService = audioService;

        LoadAchievements();
    }

    public void LoadAchievements()
    {
        var allAchievements = _achievementService.GetAllAchievements();

        UnlockedCount = _achievementService.UnlockedCount;
        TotalCount = _achievementService.TotalCount;
        ProgressText = $"{UnlockedCount}/{TotalCount}";
        OverallProgress = TotalCount > 0 ? (double)UnlockedCount / TotalCount : 0;

        Achievements.Clear();
        foreach (var achievement in allAchievements)
        {
            Achievements.Add(new AchievementDisplayModel
            {
                Id = achievement.Id,
                Icon = achievement.Icon,
                Title = _localizationService.GetString(achievement.TitleKey) ?? achievement.TitleFallback,
                Description = _localizationService.GetString(achievement.DescriptionKey) ?? achievement.DescriptionFallback,
                Category = achievement.Category,
                Progress = achievement.Progress,
                ProgressFraction = achievement.ProgressFraction,
                ProgressText = $"{achievement.CurrentValue}/{achievement.TargetValue}",
                MoneyReward = achievement.MoneyReward,
                XpReward = achievement.XpReward,
                IsUnlocked = achievement.IsUnlocked,
                IsCloseToUnlock = achievement.IsCloseToUnlock
            });
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    [RelayCommand]
    private void FilterByCategory(AchievementCategory category)
    {
        SelectedCategory = category;
        // Filtering can be done in the UI with ItemsRepeater/ListBox
    }
}

/// <summary>
/// Display model for achievements in the UI.
/// </summary>
public class AchievementDisplayModel
{
    public string Id { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public AchievementCategory Category { get; set; }
    public double Progress { get; set; }
    public double ProgressFraction { get; set; }
    public string ProgressText { get; set; } = "";
    public decimal MoneyReward { get; set; }
    public int XpReward { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsCloseToUnlock { get; set; }

    public string RewardText => MoneyReward > 0 && XpReward > 0
        ? $"+{MoneyReward:N0}€ +{XpReward} XP"
        : MoneyReward > 0
            ? $"+{MoneyReward:N0}€"
            : $"+{XpReward} XP";

    /// <summary>
    /// Background color for the achievement icon area.
    /// Gold for unlocked, grey for locked.
    /// </summary>
    public string IconBackground => IsUnlocked ? "#40FFD700" : "#20808080";

    /// <summary>
    /// Color for the achievement title.
    /// Gold for unlocked, default for locked.
    /// </summary>
    public string TitleColor => IsUnlocked ? "#FFD700" : "#AAAAAA";
}
