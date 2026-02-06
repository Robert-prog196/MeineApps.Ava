namespace FitnessRechner.Services;

/// <summary>
/// Service for undo functionality with Material Design banner
/// </summary>
public interface IUndoService<T> where T : class
{
    /// <summary>
    /// Indicates whether the undo banner is visible
    /// </summary>
    bool ShowUndoBanner { get; }

    /// <summary>
    /// The message in the undo banner
    /// </summary>
    string UndoMessage { get; }

    /// <summary>
    /// Starts an undo timeout for a deleted entry
    /// </summary>
    /// <param name="item">The deleted entry</param>
    /// <param name="message">The undo message</param>
    /// <param name="onCommit">Callback when undo timeout expires (permanent deletion)</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    Task StartUndoTimeoutAsync(T item, string message, Func<T, Task> onCommit, int timeoutMs = 5000);

    /// <summary>
    /// Undoes the deletion
    /// </summary>
    /// <returns>The restored entry or null</returns>
    T? UndoDelete();

    /// <summary>
    /// Cancels a running undo timeout
    /// </summary>
    void CancelUndo();

    /// <summary>
    /// Event that fires when the banner status changes
    /// </summary>
    event EventHandler<UndoStatusChangedEventArgs>? UndoStatusChanged;
}

/// <summary>
/// Event args for undo status changes
/// </summary>
public class UndoStatusChangedEventArgs : EventArgs
{
    public bool ShowBanner { get; init; }
    public string Message { get; init; } = string.Empty;
}
