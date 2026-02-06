namespace BomberBlast.Services;

/// <summary>
/// Manages level progress and unlocks
/// </summary>
public interface IProgressService
{
    /// <summary>Highest completed level (1-50)</summary>
    int HighestCompletedLevel { get; }

    /// <summary>Total levels available</summary>
    int TotalLevels { get; }

    /// <summary>Check if a level is unlocked</summary>
    bool IsLevelUnlocked(int level);

    /// <summary>Mark a level as completed</summary>
    void CompleteLevel(int level);

    /// <summary>Get best score for a level</summary>
    int GetLevelBestScore(int level);

    /// <summary>Set best score for a level</summary>
    void SetLevelBestScore(int level, int score);

    /// <summary>Get total stars earned (3 stars possible per level)</summary>
    int GetTotalStars();

    /// <summary>Get stars earned for a level</summary>
    int GetLevelStars(int level);

    /// <summary>Reset all progress</summary>
    void ResetProgress();
}
