namespace HandwerkerImperium.Helpers;

/// <summary>
/// Extension methods for async operations.
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    /// Safely executes a fire-and-forget task with error handling.
    /// Use this instead of discarding with '_' to ensure exceptions are logged.
    /// </summary>
    public static void FireAndForget(this Task task, Action<Exception>? onError = null)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                var exception = t.Exception.Flatten().InnerException ?? t.Exception;

                System.Diagnostics.Trace.WriteLine(
                    $"[FireAndForget Error] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");

                onError?.Invoke(exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Safely executes a fire-and-forget ValueTask with error handling.
    /// </summary>
    public static void FireAndForget(this ValueTask task, Action<Exception>? onError = null)
    {
        task.AsTask().FireAndForget(onError);
    }
}
