using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerRechner.Models;
using HandwerkerRechner.Services;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;

namespace HandwerkerRechner.ViewModels.Floor;

public partial class WallpaperCalculatorViewModel : ObservableObject
{
    private readonly CraftEngine _craftEngine;
    private readonly IProjectService _projectService;
    private readonly ILocalizationService _localization;
    private readonly ICalculationHistoryService _historyService;
    private readonly IUnitConverterService _unitConverter;
    private string? _currentProjectId;

    /// <summary>
    /// Event to request navigation (replaces Shell.Current.GoToAsync)
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Event for showing alerts/messages to the user (title, message)
    /// </summary>
    public event Action<string, string>? MessageRequested;

    [ObservableProperty]
    private bool _showSaveDialog;

    [ObservableProperty]
    private string _saveProjectName = string.Empty;

    private string DefaultProjectName => _localization.GetString("CalcWallpaper");

    /// <summary>
    /// Invoke navigation request
    /// </summary>
    private void NavigateTo(string route)
    {
        NavigationRequested?.Invoke(route);
    }

    public WallpaperCalculatorViewModel(
        CraftEngine craftEngine,
        IProjectService projectService,
        ILocalizationService localization,
        ICalculationHistoryService historyService,
        IUnitConverterService unitConverter)
    {
        _craftEngine = craftEngine;
        _projectService = projectService;
        _localization = localization;
        _historyService = historyService;
        _unitConverter = unitConverter;

        _unitConverter.UnitSystemChanged += OnUnitSystemChanged;
    }

    /// <summary>
    /// Load project data from a project ID (replaces IQueryAttributable)
    /// </summary>
    public async Task LoadFromProjectIdAsync(string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
            return;

        _currentProjectId = projectId;
        try
        {
            await LoadProjectAsync(projectId);
        }
        catch (Exception)
        {
        }
    }

    #region Input Properties

    [ObservableProperty]
    private double _wallLength = 14.0;

    [ObservableProperty]
    private double _roomHeight = 2.5;

    [ObservableProperty]
    private double _rollLength = 10.05;

    [ObservableProperty]
    private double _rollWidth = 53;

    [ObservableProperty]
    private double _patternRepeat = 0;

    #endregion

    #region Unit Labels

    public string LengthUnit => _unitConverter.GetLengthUnit();
    public string AreaUnit => _unitConverter.GetAreaUnit();
    public string RollWidthUnit => _unitConverter.CurrentSystem == UnitSystem.Metric ? "cm" : "in";

    private void OnUnitSystemChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LengthUnit));
        OnPropertyChanged(nameof(AreaUnit));
        OnPropertyChanged(nameof(RollWidthUnit));
        OnPropertyChanged(nameof(AreaDisplay));
        OnPropertyChanged(nameof(RollsNeededDisplay));
        OnPropertyChanged(nameof(StripsNeededDisplay));
        OnPropertyChanged(nameof(TotalCostDisplay));
    }

    #endregion

    #region Cost Calculation

    [ObservableProperty]
    private double _pricePerRoll = 0;

    [ObservableProperty]
    private bool _showCost = false;

    public string TotalCostDisplay => (Result != null && ShowCost && PricePerRoll > 0)
        ? $"{(Result.RollsNeeded * PricePerRoll):F2} \u20ac"
        : "";

    public string PricePerDisplay => ShowCost ? $"{_localization.GetString("PricePerRoll")}: {PricePerRoll:F2} \u20ac" : "";

    partial void OnPricePerRollChanged(double value)
    {
        ShowCost = value > 0;
        OnPropertyChanged(nameof(TotalCostDisplay));
        OnPropertyChanged(nameof(PricePerDisplay));
    }

    #endregion

    #region Result Properties

    [ObservableProperty]
    private WallpaperResult? _result;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _isCalculating;

    public string AreaDisplay => Result != null
        ? _unitConverter.FormatArea(Result.WallArea)
        : "";

    public string RollsNeededDisplay => Result != null
        ? $"{Result.RollsNeeded} {_localization.GetString("UnitRolls")}"
        : "";

    public string StripsNeededDisplay => Result != null
        ? $"{Result.StripsNeeded} {_localization.GetString("UnitStrips")}"
        : "";

    partial void OnResultChanged(WallpaperResult? value)
    {
        OnPropertyChanged(nameof(AreaDisplay));
        OnPropertyChanged(nameof(RollsNeededDisplay));
        OnPropertyChanged(nameof(StripsNeededDisplay));
        OnPropertyChanged(nameof(TotalCostDisplay));
    }

    #endregion

    [RelayCommand]
    private async Task Calculate()
    {
        if (IsCalculating) return;

        try
        {
            IsCalculating = true;

            if (WallLength <= 0 || RoomHeight <= 0 || RollLength <= 0 || RollWidth <= 0)
            {
                HasResult = false;
                MessageRequested?.Invoke(_localization.GetString("InvalidInputTitle"), _localization.GetString("ValueMustBePositive"));
                return;
            }

            Result = _craftEngine.CalculateWallpaper(WallLength / 2, 0, RoomHeight, RollLength, RollWidth, PatternRepeat);
            HasResult = true;

            await SaveToHistoryAsync();
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private async Task SaveToHistoryAsync()
    {
        try
        {
            var title = string.Format(_localization.GetString("HistoryWallHeight") ?? "{0} m wall, {1} m height", WallLength.ToString("F1"), RoomHeight.ToString("F1"));
            var data = new Dictionary<string, object>
            {
                ["WallLength"] = WallLength,
                ["RoomHeight"] = RoomHeight,
                ["RollLength"] = RollLength,
                ["RollWidth"] = RollWidth,
                ["PatternRepeat"] = PatternRepeat,
                ["PricePerRoll"] = PricePerRoll,
                ["Result"] = Result != null ? new Dictionary<string, object>
                {
                    ["WallArea"] = Result.WallArea,
                    ["StripsNeeded"] = Result.StripsNeeded,
                    ["RollsNeeded"] = Result.RollsNeeded
                } : new Dictionary<string, object>()
            };

            await _historyService.AddCalculationAsync("WallpaperCalculator", title, data);
        }
        catch (Exception)
        {
        }
    }

    [RelayCommand]
    private void Reset()
    {
        WallLength = 14.0;
        RoomHeight = 2.5;
        RollLength = 10.05;
        RollWidth = 53;
        PatternRepeat = 0;
        PricePerRoll = 0;
        Result = null;
        HasResult = false;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateTo("..");
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        if (!HasResult) return;

        SaveProjectName = _currentProjectId != null ? "" : DefaultProjectName;
        ShowSaveDialog = true;
    }

    [RelayCommand]
    private async Task ConfirmSaveProject()
    {
        var name = SaveProjectName;
        if (string.IsNullOrWhiteSpace(name))
            name = DefaultProjectName;

        ShowSaveDialog = false;

        try
        {
            var project = new Project
            {
                Name = name,
                CalculatorType = CalculatorType.Wallpaper
            };

            if (!string.IsNullOrEmpty(_currentProjectId))
            {
                project.Id = _currentProjectId;
            }

            var data = new Dictionary<string, object>
            {
                ["WallLength"] = WallLength,
                ["RoomHeight"] = RoomHeight,
                ["RollLength"] = RollLength,
                ["RollWidth"] = RollWidth,
                ["PatternRepeat"] = PatternRepeat,
                ["PricePerRoll"] = PricePerRoll
            };

            project.SetData(data);
            await _projectService.SaveProjectAsync(project);
            _currentProjectId = project.Id;

            MessageRequested?.Invoke(
                _localization.GetString("Success"),
                _localization.GetString("ProjectSaved"));
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(
                _localization.GetString("Error"),
                _localization.GetString("ProjectSaveFailed"));
        }
    }

    [RelayCommand]
    private void CancelSaveProject()
    {
        ShowSaveDialog = false;
        SaveProjectName = string.Empty;
    }

    private async Task LoadProjectAsync(string projectId)
    {
        try
        {
            var project = await _projectService.LoadProjectAsync(projectId);
            if (project == null)
                return;

            WallLength = project.GetValue("WallLength", 14.0);
            RoomHeight = project.GetValue("RoomHeight", 2.5);
            RollLength = project.GetValue("RollLength", 10.05);
            RollWidth = project.GetValue("RollWidth", 53.0);
            PatternRepeat = project.GetValue("PatternRepeat", 0.0);
            PricePerRoll = project.GetValue("PricePerRoll", 0.0);

            await Calculate();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Cleanup when ViewModel is disposed
    /// </summary>
    public void Cleanup()
    {
        _unitConverter.UnitSystemChanged -= OnUnitSystemChanged;
    }
}
