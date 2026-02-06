using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Handles saving and loading game state to persistent storage.
/// </summary>
public interface ISaveGameService
{
    /// <summary>
    /// Whether a save file exists.
    /// </summary>
    bool SaveExists { get; }

    /// <summary>
    /// Path to the save file.
    /// </summary>
    string SaveFilePath { get; }

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Loads the saved game state.
    /// Returns null if no save exists or loading fails.
    /// </summary>
    Task<GameState?> LoadAsync();

    /// <summary>
    /// Deletes the save file.
    /// </summary>
    Task DeleteSaveAsync();

    /// <summary>
    /// Exports the save data as a JSON string (for backup/sharing).
    /// </summary>
    Task<string> ExportSaveAsync();

    /// <summary>
    /// Imports save data from a JSON string.
    /// </summary>
    Task<bool> ImportSaveAsync(string json);
}
