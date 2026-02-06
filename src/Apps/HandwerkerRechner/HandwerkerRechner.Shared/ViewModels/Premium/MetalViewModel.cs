using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerRechner.Models;
using HandwerkerRechner.Services;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;

namespace HandwerkerRechner.ViewModels.Premium;

public partial class MetalViewModel : ObservableObject
{
    private readonly CraftEngine _engine;
    private readonly IProjectService _projectService;
    private readonly ILocalizationService _localization;
    private readonly ICalculationHistoryService _historyService;
    private string? _currentProjectId;

    public event Action<string>? NavigationRequested;
    public event Action<string, string>? MessageRequested;
    private void NavigateTo(string route) => NavigationRequested?.Invoke(route);

    public MetalViewModel(
        CraftEngine engine,
        IProjectService projectService,
        ILocalizationService localization,
        ICalculationHistoryService historyService)
    {
        _engine = engine;
        _projectService = projectService;
        _localization = localization;
        _historyService = historyService;
    }

    /// <summary>
    /// Loads project data by ID (replaces IQueryAttributable)
    /// </summary>
    public async Task LoadFromProjectIdAsync(string projectId)
    {
        if (!string.IsNullOrEmpty(projectId))
        {
            _currentProjectId = projectId;
            await LoadProjectAsync(projectId);
        }
    }

    // Save Dialog
    [ObservableProperty] private bool _showSaveDialog;
    [ObservableProperty] private string _saveProjectName = string.Empty;

    private string DefaultProjectName => Calculators[SelectedCalculator];

    // Calculator Selection
    [ObservableProperty] private int _selectedCalculator;

    partial void OnSelectedCalculatorChanged(int value)
    {
        // Reset cost values when switching calculators to prevent state leaking
        if (value != 0) // Metal Weight
        {
            PricePerKg = 0;
        }
        // Clear results when switching
        HasResult = false;
    }

    public List<string> Calculators => [
        _localization.GetString("MetalWeight"),
        _localization.GetString("ThreadDrill")
    ];

    // Metal Weight Inputs
    [ObservableProperty] private int _selectedMetal;
    [ObservableProperty] private int _selectedProfile;
    [ObservableProperty] private double _length = 1.0;
    [ObservableProperty] private double _dimension1 = 20;
    [ObservableProperty] private double _dimension2 = 10;
    [ObservableProperty] private double _wallThickness = 2;

    public List<string> Metals => [
        _localization.GetString("MetalSteel"),
        _localization.GetString("MetalStainlessSteel"),
        _localization.GetString("MetalAluminum"),
        _localization.GetString("MetalCopper"),
        _localization.GetString("MetalBrass"),
        _localization.GetString("MetalBronze")
    ];
    public List<string> Profiles => [
        _localization.GetString("ProfileRoundBar"),
        _localization.GetString("ProfileFlatBar"),
        _localization.GetString("ProfileSquareBar"),
        _localization.GetString("ProfileRoundTube"),
        _localization.GetString("ProfileSquareTube"),
        _localization.GetString("ProfileAngle")
    ];

    // Thread Drill Inputs
    [ObservableProperty] private int _selectedThread;
    public List<string> ThreadSizes { get; } = ["M3", "M4", "M5", "M6", "M8", "M10", "M12", "M14", "M16", "M18", "M20", "M22", "M24", "M27", "M30"];

    // Results
    [ObservableProperty] private MetalWeightResult? _weightResult;
    [ObservableProperty] private ThreadDrillResult? _threadResult;
    [ObservableProperty] private bool _hasResult;

    [ObservableProperty]
    private bool _isCalculating;

    #region Cost Calculation

    // Metallgewicht: Preis pro kg
    [ObservableProperty]
    private double _pricePerKg = 0;

    [ObservableProperty]
    private bool _showMetalCost = false;

    public string MetalCostDisplay => (ShowMetalCost && PricePerKg > 0 && WeightResult != null && WeightResult.Weight > 0)
        ? $"{_localization.GetString("ResultMaterialCost")}: {(WeightResult.Weight * PricePerKg):F2} \u20ac"
        : "";

    partial void OnPricePerKgChanged(double value)
    {
        ShowMetalCost = value > 0;
        OnPropertyChanged(nameof(MetalCostDisplay));
    }

    partial void OnWeightResultChanged(MetalWeightResult? value)
    {
        OnPropertyChanged(nameof(MetalCostDisplay));
    }

    // Gewindebohrer: Keine Kostenberechnung (nur Tabelle)

    #endregion

    [RelayCommand]
    private async Task Calculate()
    {
        if (IsCalculating) return;

        try
        {
            IsCalculating = true;

            switch (SelectedCalculator)
            {
                case 0: // Metal Weight
                    if (Length <= 0 || Dimension1 <= 0)
                    {
                        HasResult = false;
                        MessageRequested?.Invoke(
                            _localization.GetString("InvalidInputTitle"),
                            _localization.GetString("ValueMustBePositive"));
                        return;
                    }
                    var metal = (MetalType)SelectedMetal;
                    var profile = (ProfileType)SelectedProfile;
                    WeightResult = _engine.CalculateMetalWeight(metal, profile, Length, Dimension1, Dimension2, WallThickness);
                    ThreadResult = null;
                    break;

                case 1: // Thread Drill
                    var threadSize = ThreadSizes[SelectedThread];
                    ThreadResult = _engine.GetThreadDrill(threadSize);
                    WeightResult = null;
                    break;
            }
            HasResult = true;

            // Save to history
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
            string calcType, title;
            Dictionary<string, object> data;

            switch (SelectedCalculator)
            {
                case 0: // Metal Weight
                    calcType = "MetalWeightCalculator";
                    title = $"{Metals[SelectedMetal]}, {Profiles[SelectedProfile]}, {Length}m";
                    data = new Dictionary<string, object>
                    {
                        ["SelectedMetal"] = SelectedMetal,
                        ["SelectedProfile"] = SelectedProfile,
                        ["Length"] = Length,
                        ["Dimension1"] = Dimension1,
                        ["Dimension2"] = Dimension2,
                        ["WallThickness"] = WallThickness,
                        ["Result"] = WeightResult != null ? new Dictionary<string, object>
                        {
                            ["Weight"] = WeightResult.Weight,
                            ["Volume"] = WeightResult.Volume
                        } : new Dictionary<string, object>()
                    };
                    break;

                default: // Thread Drill
                    calcType = "ThreadDrillCalculator";
                    title = $"{_localization.GetString("HistoryThreadSize")} {ThreadSizes[SelectedThread]}";
                    data = new Dictionary<string, object>
                    {
                        ["SelectedThread"] = SelectedThread,
                        ["Result"] = ThreadResult != null ? new Dictionary<string, object>
                        {
                            ["ThreadSize"] = ThreadResult.ThreadSize,
                            ["DrillSize"] = ThreadResult.DrillSize,
                            ["Found"] = ThreadResult.Found
                        } : new Dictionary<string, object>()
                    };
                    break;
            }

            await _historyService.AddCalculationAsync(calcType, title, data);
        }
        catch (Exception)
        {
            // Silently ignore history save errors
        }
    }

    [RelayCommand]
    private void Reset()
    {
        SelectedMetal = 0;
        SelectedProfile = 0;
        Length = 1.0;
        Dimension1 = 20;
        Dimension2 = 10;
        WallThickness = 2;
        SelectedThread = 0;
        PricePerKg = 0;
        WeightResult = null;
        ThreadResult = null;
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
        SaveProjectName = string.Empty;
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
            var calcType = SelectedCalculator switch
            {
                0 => CalculatorType.MetalWeight,
                1 => CalculatorType.ThreadDrill,
                _ => CalculatorType.MetalWeight
            };

            var project = new Project
            {
                Name = name,
                CalculatorType = calcType
            };

            if (!string.IsNullOrEmpty(_currentProjectId))
            {
                project.Id = _currentProjectId;
            }

            var data = new Dictionary<string, object>
            {
                ["SelectedCalculator"] = SelectedCalculator,
                // Metal Weight
                ["SelectedMetal"] = SelectedMetal,
                ["SelectedProfile"] = SelectedProfile,
                ["Length"] = Length,
                ["Dimension1"] = Dimension1,
                ["Dimension2"] = Dimension2,
                ["WallThickness"] = WallThickness,
                ["PricePerKg"] = PricePerKg,
                // Thread Drill
                ["SelectedThread"] = SelectedThread
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

            SelectedCalculator = project.GetValue("SelectedCalculator", 0);

            // Metal Weight
            SelectedMetal = project.GetValue("SelectedMetal", 0);
            SelectedProfile = project.GetValue("SelectedProfile", 0);
            Length = project.GetValue("Length", 1.0);
            Dimension1 = project.GetValue("Dimension1", 20.0);
            Dimension2 = project.GetValue("Dimension2", 10.0);
            WallThickness = project.GetValue("WallThickness", 2.0);
            PricePerKg = project.GetValue("PricePerKg", 0.0);

            // Thread Drill
            SelectedThread = project.GetValue("SelectedThread", 0);

            await Calculate();
        }
        catch (Exception)
        {
            // Silently ignore load errors
        }
    }
}
