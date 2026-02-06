using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Manages the in-game tutorial system.
/// </summary>
public interface ITutorialService
{
    /// <summary>
    /// Event fired when the tutorial step changes.
    /// </summary>
    event EventHandler<TutorialStep>? StepChanged;

    /// <summary>
    /// Event fired when the tutorial is completed.
    /// </summary>
    event EventHandler? TutorialCompleted;

    /// <summary>
    /// Whether the tutorial has been completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Whether the tutorial is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Current tutorial step (0-based).
    /// </summary>
    int CurrentStepIndex { get; }

    /// <summary>
    /// Gets the current tutorial step.
    /// </summary>
    TutorialStep? CurrentStep { get; }

    /// <summary>
    /// Total number of tutorial steps.
    /// </summary>
    int TotalSteps { get; }

    /// <summary>
    /// Starts the tutorial from the beginning.
    /// </summary>
    void StartTutorial();

    /// <summary>
    /// Advances to the next tutorial step.
    /// </summary>
    void NextStep();

    /// <summary>
    /// Goes back to the previous step.
    /// </summary>
    void PreviousStep();

    /// <summary>
    /// Skips the tutorial entirely.
    /// </summary>
    void SkipTutorial();

    /// <summary>
    /// Resets the tutorial (for testing or replay).
    /// </summary>
    void ResetTutorial();
}
