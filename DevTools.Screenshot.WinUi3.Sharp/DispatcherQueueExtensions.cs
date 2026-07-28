using Microsoft.UI.Dispatching;

namespace DevTools.Screenshot.WinUi3.Sharp;

internal static class DispatcherQueueExtensions
{
    #region Error messages

    private const string EnqueueFailedMessage = "Failed to enqueue work on the UI thread.";

    #endregion

    public static Task<T> EnqueueAsync<T>(this DispatcherQueue queue, Func<Task<T>> callback)
    {
        if (queue.HasThreadAccess)
            return callback();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(async () =>
        {
            try
            {
                tcs.SetResult(await callback());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            throw new InvalidOperationException(EnqueueFailedMessage);
        }

        return tcs.Task;
    }
}
