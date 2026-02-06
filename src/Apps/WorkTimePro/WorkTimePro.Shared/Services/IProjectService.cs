using WorkTimePro.Models;

namespace WorkTimePro.Services;

/// <summary>
/// Service for project tracking (Premium feature)
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Get all active projects
    /// </summary>
    Task<List<Project>> GetProjectsAsync(bool includeInactive = false);

    /// <summary>
    /// Get project by ID
    /// </summary>
    Task<Project?> GetProjectAsync(int id);

    /// <summary>
    /// Save project
    /// </summary>
    Task SaveProjectAsync(Project project);

    /// <summary>
    /// Delete project
    /// </summary>
    Task DeleteProjectAsync(int id);

    /// <summary>
    /// Add time to project
    /// </summary>
    Task<ProjectTimeEntry> AddProjectTimeAsync(int projectId, DateTime date, int minutes, string? note = null);

    /// <summary>
    /// Delete project time entry
    /// </summary>
    Task DeleteProjectTimeEntryAsync(int entryId);

    /// <summary>
    /// Get hours per project for a period
    /// </summary>
    Task<Dictionary<Project, double>> GetProjectHoursAsync(DateTime start, DateTime end);

    /// <summary>
    /// Get time entries for a project
    /// </summary>
    Task<List<ProjectTimeEntry>> GetProjectTimeEntriesAsync(int projectId, DateTime start, DateTime end);

    /// <summary>
    /// Get statistics for a project
    /// </summary>
    Task<ProjectStatistics> GetProjectStatisticsAsync(int projectId);
}

/// <summary>
/// Project statistics
/// </summary>
public class ProjectStatistics
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public double TotalHours { get; set; }
    public double ThisMonthHours { get; set; }
    public double LastMonthHours { get; set; }
    public int EntryCount { get; set; }
    public DateTime? FirstEntry { get; set; }
    public DateTime? LastEntry { get; set; }
}
