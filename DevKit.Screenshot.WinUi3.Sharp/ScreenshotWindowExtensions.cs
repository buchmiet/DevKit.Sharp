using DevKit.Screenshot.Sharp;
using Microsoft.UI.Xaml;

namespace DevKit.Screenshot.WinUi3.Sharp;

/// <summary>CI/tooling hook that captures a window without touching app code.</summary>
public static class ScreenshotWindowExtensions
{
    /// <summary>
    /// When <paramref name="options"/> are enabled, captures <paramref name="window"/> once it is
    /// first activated and rendered. With <see cref="ScreenshotOptions.ExitAfterCapture"/> the app
    /// exits afterwards — normally on success, or with <see cref="ScreenshotExitCodes.CaptureFailed"/>
    /// when the capture fails (the failure is written to stderr). Call before
    /// <see cref="Window.Activate"/>.
    /// </summary>
    public static Window AttachScreenshot(this Window window, ScreenshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsEnabled)
            return window;

        options.EnsureEnabled();
        var scheduled = false;

        window.Activated += OnActivated;
        return window;

        async void OnActivated(object sender, WindowActivatedEventArgs args)
        {
            if (scheduled || args.WindowActivationState == WindowActivationState.Deactivated)
                return;

            scheduled = true;
            window.Activated -= OnActivated;

            try
            {
                await WinUiScreenshot.CaptureCoreAsync(window, options, CancellationToken.None);

                if (options.ExitAfterCapture)
                    Application.Current.Exit();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DevKit.Screenshot] Main-window capture failed: {ex}");

                if (options.ExitAfterCapture)
                    Environment.Exit(ScreenshotExitCodes.CaptureFailed);
            }
        }
    }
}
