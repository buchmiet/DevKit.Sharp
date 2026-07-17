using Microsoft.UI.Dispatching;

namespace DevTools.ScreenShot.WinUi3.Sharp;

internal static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this DispatcherQueue queue, Func<Task> callback)
    {
        if (queue.HasThreadAccess)
            return callback();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(async () =>
        {
            try
            {
                await callback();
                tcs.SetResult();
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
