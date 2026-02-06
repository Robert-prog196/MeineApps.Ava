using FitnessRechner.Models;

namespace FitnessRechner.Services;

/// <summary>
/// Service for fitness tracking (weight, BMI, water)
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Adds a new tracking entry
    /// </summary>
    Task<TrackingEntry> AddEntryAsync(TrackingEntry entry);

    /// <summary>
    /// Updates an entry
    /// </summary>
    Task<bool> UpdateEntryAsync(TrackingEntry entry);

    /// <summary>
    /// Deletes an entry
    /// </summary>
    Task<bool> DeleteEntryAsync(string id);

    /// <summary>
    /// Gets all entries of a type
    /// </summary>
    Task<IReadOnlyList<TrackingEntry>> GetEntriesAsync(TrackingType type, int limit = 30);

    /// <summary>
    /// Gets entries for a time range
    /// </summary>
    Task<IReadOnlyList<TrackingEntry>> GetEntriesAsync(TrackingType type, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the latest entry of a type
    /// </summary>
    Task<TrackingEntry?> GetLatestEntryAsync(TrackingType type);

    /// <summary>
    /// Gets statistics for a tracking type
    /// </summary>
    Task<TrackingStats?> GetStatsAsync(TrackingType type, int days = 30);

    /// <summary>
    /// Clears all entries (reset)
    /// </summary>
    Task ClearAllAsync();
}
