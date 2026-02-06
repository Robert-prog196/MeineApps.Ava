using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Manages the in-game tutorial system.
/// </summary>
public class TutorialService : ITutorialService
{
    private readonly IGameStateService _gameStateService;
    private readonly List<TutorialStep> _steps;
    private bool _isActive;

    public event EventHandler<TutorialStep>? StepChanged;
    public event EventHandler? TutorialCompleted;

    public TutorialService(IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
        _steps = TutorialSteps.GetAll();
    }

    public bool IsCompleted => _gameStateService.State.TutorialCompleted;

    public bool IsActive => _isActive && !IsCompleted;

    public int CurrentStepIndex => _gameStateService.State.TutorialStep;

    public TutorialStep? CurrentStep =>
        _isActive && CurrentStepIndex >= 0 && CurrentStepIndex < _steps.Count
            ? _steps[CurrentStepIndex]
            : null;

    public int TotalSteps => _steps.Count;

    public void StartTutorial()
    {
        if (IsCompleted) return;

        _gameStateService.State.TutorialStep = 0;
        _isActive = true;

        if (CurrentStep != null)
        {
            StepChanged?.Invoke(this, CurrentStep);
        }
    }

    public void NextStep()
    {
        if (!_isActive) return;

        var nextIndex = CurrentStepIndex + 1;

        if (nextIndex >= _steps.Count)
        {
            // Tutorial complete
            CompleteTutorial();
        }
        else
        {
            _gameStateService.State.TutorialStep = nextIndex;
            StepChanged?.Invoke(this, _steps[nextIndex]);
        }
    }

    public void PreviousStep()
    {
        if (!_isActive || CurrentStepIndex <= 0) return;

        var prevIndex = CurrentStepIndex - 1;
        _gameStateService.State.TutorialStep = prevIndex;
        StepChanged?.Invoke(this, _steps[prevIndex]);
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    public void ResetTutorial()
    {
        _gameStateService.State.TutorialCompleted = false;
        _gameStateService.State.TutorialStep = 0;
        _isActive = false;
    }

    private void CompleteTutorial()
    {
        _gameStateService.State.TutorialCompleted = true;
        _gameStateService.State.TutorialStep = _steps.Count;
        _isActive = false;
        TutorialCompleted?.Invoke(this, EventArgs.Empty);
    }
}
