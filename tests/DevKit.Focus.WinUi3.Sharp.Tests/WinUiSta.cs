using System.Runtime.ExceptionServices;
using DevKit.Focus.WinUi3.Sharp;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace DevKit.Focus.WinUi3.Sharp.Tests;

internal static class WinUiSta
{
    public static T Run<T>(Func<T> work)
    {
        T result = default!;
        Exception? error = null;
        using var done = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            DispatcherQueueController? controller = null;
            try
            {
                controller = DispatcherQueueController.CreateOnCurrentThread();
                using var _ = WindowsXamlManager.InitializeForCurrentThread();
                result = work();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                try
                {
                    controller?.ShutdownQueue();
                }
                catch
                {
                    // The queue may already be draining; the STA thread is about to exit.
                }

                done.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        if (!done.Wait(TimeSpan.FromSeconds(30)))
            throw new TimeoutException("WinUI STA work timed out.");
        if (error is not null)
            ExceptionDispatchInfo.Capture(error).Throw();
        return result;
    }
}

internal readonly record struct WinUiTreeFocusSnapshot(
    bool CanReceiveFocus,
    bool TryFocusDefault,
    bool ContainsKeyboardFocus);

internal static class WinUiTreeFocusHost
{
    public static WinUiTreeFocusSnapshot FocusDefaultOnShownButton()
    {
        var queue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("DispatcherQueue is required.");

        var button = new Button { Content = "Go", IsTabStop = true };
        var window = new Window { Content = button };
        WaitUntilLoaded(queue, button, () => window.Activate());

        var scope = button.CreateKeyboardFocusScope("btn");
        var focused = scope.TryFocusDefault();
        var snapshot = new WinUiTreeFocusSnapshot(
            scope.CanReceiveFocus,
            focused,
            scope.ContainsKeyboardFocus);

        window.Close();
        return snapshot;
    }

    public static bool IncludeCountsFocusInAdditionalRoot()
    {
        var queue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("DispatcherQueue is required.");

        var left = new Button { Name = "left", Content = "L", IsTabStop = true };
        var right = new Button { Name = "right", Content = "R", IsTabStop = true };
        var panel = new StackPanel();
        panel.Children.Add(left);
        panel.Children.Add(right);
        var window = new Window { Content = panel };
        WaitUntilLoaded(queue, panel, () => window.Activate());

        right.Focus(FocusState.Programmatic);

        var scope = WinUi3KeyboardFocusScope
            .For(left)
            .Named("editor")
            .Include(right)
            .Build();

        var contains = scope.ContainsKeyboardFocus;
        window.Close();
        return contains;
    }

    private static void WaitUntilLoaded(DispatcherQueue queue, FrameworkElement element, Action show)
    {
        using var loaded = new ManualResetEventSlim(false);
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            element.Loaded -= OnLoaded;
            queue.EnqueueEventLoopExit();
            loaded.Set();
        }

        element.Loaded += OnLoaded;
        var timer = queue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(3);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            queue.EnqueueEventLoopExit();
        };
        timer.Start();
        show();
        queue.RunEventLoop();
        timer.Stop();
        element.Loaded -= OnLoaded;
        if (!loaded.IsSet)
            throw new TimeoutException("WinUI element did not raise Loaded.");
    }
}
