using HandwerkerRechner.Models;

namespace HandwerkerRechner.Services;

/// <summary>
/// Service for project storage and management
/// </summary>
public interface IProjectService
{
    Task SaveProjectAsync(Project project);
    Task<List<Project>> LoadAllProjectsAsync();
    Task<Project?> LoadProjectAsync(string projectId);
    Task DeleteProjectAsync(string projectId);
    Task<List<Project>> LoadProjectsByTypeAsync(CalculatorType calculatorType);
}
