namespace DevTools.Screenshot.Sharp;

/// <summary>
/// Captures the app's main window to a PNG file. Implementations marshal to the UI thread
/// themselves, so this is safe to call from anywhere. Capturing never exits the app —
/// exit policy belongs to the per-framework <c>AttachScreenshot</c> hooks.
/// </summary>
public interface IScreenshot
{
    /// <summary>
    /// Waits for the main window to lay out and render, applies <see cref="ScreenshotOptions.Delay"/>,
    /// and writes a PNG to <see cref="ScreenshotOptions.OutputPath"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The options are disabled, or no capturable main window is available.
    /// </exception>
    Task<ScreenshotResult> CaptureMainWindowAsync(ScreenshotOptions options, CancellationToken cancellationToken = default);
}

/// <summary>Convenience overloads for <see cref="IScreenshot"/>.</summary>
public static class ScreenshotCaptureExtensions
{
    /// <summary>Captures the main window to <paramref name="outputPath"/> using default options.</summary>
    public static Task<ScreenshotResult> CaptureMainWindowAsync(
        this IScreenshot screenshot,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(screenshot);
        return screenshot.CaptureMainWindowAsync(new ScreenshotOptions { OutputPath = outputPath }, cancellationToken);
    }
}
