using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BomberBlast.ViewModels;

/// <summary>
/// ViewModel for the pause overlay.
/// Provides commands for resume, restart, settings, and quit.
/// </summary>
public partial class PauseViewModel : ObservableObject
{
    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event to request navigation. Parameter is the route string.
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Event raised when the player wants to resume the game.
    /// </summary>
    public event Action? ResumeRequested;

    /// <summary>
    /// Event raised when the player wants to restart the current game.
    /// </summary>
    public event Action? RestartRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public PauseViewModel()
    {
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void Resume()
    {
        ResumeRequested?.Invoke();
        NavigationRequested?.Invoke("..");
    }

    [RelayCommand]
    private void Restart()
    {
        RestartRequested?.Invoke();
        NavigationRequested?.Invoke("..");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        NavigationRequested?.Invoke("Settings");
    }

    [RelayCommand]
    private void Quit()
    {
        NavigationRequested?.Invoke("//MainMenu");
    }
}
