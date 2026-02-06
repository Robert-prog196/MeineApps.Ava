namespace FitnessRechner.Services;

/// <summary>
/// Reusable service for undo functionality
/// </summary>
public class UndoService<T> : IUndoService<T> where T : class
{
    private T? _deletedItem;
    private CancellationTokenSource? _undoCancellation;
    private bool _showUndoBanner;
    private string _undoMessage = string.Empty;

    public bool ShowUndoBanner
    {
        get => _showUndoBanner;
        private set
        {
            if (_showUndoBanner != value)
            {
                _showUndoBanner = value;
                UndoStatusChanged?.Invoke(this, new UndoStatusChangedEventArgs
                {
                    ShowBanner = value,
                    Message = _undoMessage
                });
            }
        }
    }

    public string UndoMessage
    {
        get => _undoMessage;
        private set
        {
            if (_undoMessage != value)
            {
                _undoMessage = value;
                UndoStatusChanged?.Invoke(this, new UndoStatusChangedEventArgs
                {
                    ShowBanner = _showUndoBanner,
                    Message = value
                });
            }
        }
    }

    public event EventHandler<UndoStatusChangedEventArgs>? UndoStatusChanged;

    public async Task StartUndoTimeoutAsync(T item, string message, Func<T, Task> onCommit, int timeoutMs = 5000)
    {
        // Cancel previous undo if exists
        _undoCancellation?.Cancel();
        _undoCancellation = new CancellationTokenSource();

        // Store deleted item
        _deletedItem = item;

        // Show undo banner
        UndoMessage = message;
        ShowUndoBanner = true;

        try
        {
            // Wait for undo
            await Task.Delay(timeoutMs, _undoCancellation.Token);

            // If not cancelled, commit deletion permanently
            await onCommit(item);
            _deletedItem = null;
        }
        catch (TaskCanceledException)
        {
            // Undo was triggered, nothing to do
        }
        finally
        {
            ShowUndoBanner = false;
        }
    }

    public T? UndoDelete()
    {
        if (_deletedItem != null)
        {
            _undoCancellation?.Cancel();
            var item = _deletedItem;
            _deletedItem = null;
            ShowUndoBanner = false;
            return item;
        }
        return null;
    }

    public void CancelUndo()
    {
        _undoCancellation?.Cancel();
        _deletedItem = null;
        ShowUndoBanner = false;
    }

    // NOTE: No finalizer - CancellationTokenSource will be cleaned up when service is no longer referenced
}
