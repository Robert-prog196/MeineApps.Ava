namespace MeineApps.Core.Ava.Services;

/// <summary>
/// Service for storing and retrieving app preferences
/// </summary>
public interface IPreferencesService
{
    /// <summary>
    /// Get a preference value
    /// </summary>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Set a preference value
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Check if a preference exists
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Remove a preference
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Clear all preferences
    /// </summary>
    void Clear();
}
