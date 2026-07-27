using Avalonia.Controls.ApplicationLifetimes;
using DevTools.Screenshot.Sharp;

namespace DevTools.Screenshot.Avalonia.Sharp;

/// <summary>CI/tooling hook that captures the main window without touching app code.</summary>
public static class ScreenshotLifetimeExtensions
{
    /// <summary>
    /// When <paramref name="options"/> are enabled, captures the main window once it has opened
    /// and rendered. With <see cref="ScreenshotOptions.ExitAfterCapture"/> the app is shut down
    /// afterwards — with exit code 0 on success or <see cref="ScreenshotExitCodes.CaptureFailed"/>
    /// on failure (the failure is written to stderr). Call after assigning
    /// <see cref="IClassicDesktopStyleApplicationLifetime.MainWindow"/>.
    /// </summary>
    public static IClassicDesktopStyleApplicationLifetime AttachScreenshot(
        this IClassicDesktopStyleApplicationLifetime desktop,
        ScreenshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(desktop);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsEnabled)
            return desktop;

        var window = desktop.MainWindow
            ?? throw new InvalidOperationException(
                "Assign desktop.MainWindow before calling AttachScreenshot.");
        options.EnsureEnabled();

        if (window.IsLoaded)
        {
            _ = CaptureAndMaybeExitAsync();
        }
        else
        {
            window.Opened += OnOpened;
        }

        return desktop;

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            _ = CaptureAndMaybeExitAsync();
        }

        async Task CaptureAndMaybeExitAsync()
        {
            try
            {
                await AvaloniaScreenshot.CaptureCoreAsync(window, options, CancellationToken.None);

                if (options.ExitAfterCapture)
                    desktop.Shutdown();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DevTools.Screenshot] Main-window capture failed: {ex}");

                if (options.ExitAfterCapture)
                    desktop.Shutdown(ScreenshotExitCodes.CaptureFailed);
            }
        }
    }
}
