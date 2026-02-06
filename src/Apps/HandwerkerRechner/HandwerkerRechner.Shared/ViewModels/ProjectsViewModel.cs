using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerRechner.Models;
using HandwerkerRechner.Services;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerRechner.ViewModels;

/// <summary>
/// ViewModel for the project management page
/// </summary>
public partial class ProjectsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ILocalizationService _localization;

    /// <summary>
    /// Raised when the VM wants to navigate to a page.
    /// The string parameter is the route/page name (may include query params).
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Event for showing alerts/messages to the user (title, message)
    /// </summary>
    public event Action<string, string>? MessageRequested;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasProjects;

    [ObservableProperty]
    private bool _showDeleteConfirmation;

    [ObservableProperty]
    private Project? _projectToDelete;

    /// <summary>
    /// True when not loading and no projects exist
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasProjects;

    public ProjectsViewModel(IProjectService projectService, ILocalizationService localization)
    {
        _projectService = projectService;
        _localization = localization;
    }

    private void NavigateTo(string route) => NavigationRequested?.Invoke(route);

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var projects = await _projectService.LoadAllProjectsAsync();
            Projects = new ObservableCollection<Project>(projects);
            HasProjects = Projects.Count > 0;
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(_localization.GetString("Error"), _localization.GetString("ProjectLoadFailed"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void LoadProject(Project? project)
    {
        if (project == null) return;

        try
        {
            var route = GetRouteForCalculatorType(project.CalculatorType);
            if (!string.IsNullOrEmpty(route))
            {
                NavigateTo($"{route}?projectId={project.Id}");
            }
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(_localization.GetString("Error"), _localization.GetString("ProjectLoadFailed"));
        }
    }

    [RelayCommand]
    private void DeleteProject(Project? project)
    {
        if (project == null) return;
        ProjectToDelete = project;
        ShowDeleteConfirmation = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteProject()
    {
        if (ProjectToDelete == null) return;

        ShowDeleteConfirmation = false;

        try
        {
            await _projectService.DeleteProjectAsync(ProjectToDelete.Id);
            Projects.Remove(ProjectToDelete);
            HasProjects = Projects.Count > 0;
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(
                _localization.GetString("Error"),
                _localization.GetString("ProjectDeleteFailed"));
        }
        finally
        {
            ProjectToDelete = null;
        }
    }

    [RelayCommand]
    private void CancelDeleteProject()
    {
        ShowDeleteConfirmation = false;
        ProjectToDelete = null;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateTo("..");
    }

    private static string GetRouteForCalculatorType(CalculatorType calculatorType) => calculatorType switch
    {
        // FREE
        CalculatorType.Tiles => "TileCalculatorPage",
        CalculatorType.Wallpaper => "WallpaperCalculatorPage",
        CalculatorType.Paint => "PaintCalculatorPage",
        CalculatorType.Flooring => "FlooringCalculatorPage",

        // PREMIUM
        CalculatorType.DrywallFraming => "DrywallPage",
        CalculatorType.Baseboard => "DrywallPage",
        CalculatorType.VoltageDrop => "ElectricalPage",
        CalculatorType.PowerCost => "ElectricalPage",
        CalculatorType.OhmsLaw => "ElectricalPage",
        CalculatorType.MetalWeight => "MetalPage",
        CalculatorType.ThreadDrill => "MetalPage",
        CalculatorType.Paving => "GardenPage",
        CalculatorType.Soil => "GardenPage",
        CalculatorType.PondLiner => "GardenPage",
        CalculatorType.RoofPitch => "RoofSolarPage",
        CalculatorType.RoofTiles => "RoofSolarPage",
        CalculatorType.SolarYield => "RoofSolarPage",

        _ => string.Empty
    };
}
