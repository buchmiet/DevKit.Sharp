using Microsoft.UI.Dispatching;

namespace DevTools.Screenshot.WinUi3.Sharp;

internal static class DispatcherQueueExtensions
{
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
            throw new InvalidOperationException("Failed to enqueue work on the UI thread.");
        }

        return tcs.Task;
    }
}
