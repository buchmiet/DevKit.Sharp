namespace DevKit.Screenshot.Sharp;

/// <summary>
/// Captures the app's main window to a PNG file or the system clipboard. Implementations marshal to
/// the UI thread themselves, so this is safe to call from anywhere. Capturing never exits the app —
/// exit policy belongs to the per-framework <c>AttachScreenshot</c> hooks.
/// </summary>
public interface IScreenshot
{
    /// <summary>
    /// Waits for the main window to lay out and render, applies <see cref="ScreenshotOptions.Delay"/>,
    /// and writes a PNG to <see cref="ScreenshotOptions.OutputPath"/> or copies it to the clipboard
    /// when <see cref="ScreenshotOptions.CopyToClipboard"/> is true.
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
        if (screenshot is null)
            throw new ArgumentNullException(nameof(screenshot));

        return screenshot.CaptureMainWindowAsync(new ScreenshotOptions { OutputPath = outputPath }, cancellationToken);
    }

    /// <summary>Captures the main window and places it on the system clipboard.</summary>
    public static Task<ScreenshotResult> CaptureMainWindowToClipboardAsync(
        this IScreenshot screenshot,
        CancellationToken cancellationToken = default)
    {
        if (screenshot is null)
            throw new ArgumentNullException(nameof(screenshot));

        return screenshot.CaptureMainWindowAsync(
            new ScreenshotOptions { CopyToClipboard = true },
            cancellationToken);
    }

    /// <summary>Captures the main window and places it on the system clipboard after <paramref name="delay"/>.</summary>
    public static Task<ScreenshotResult> CaptureMainWindowToClipboardAsync(
        this IScreenshot screenshot,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        if (screenshot is null)
            throw new ArgumentNullException(nameof(screenshot));

        return screenshot.CaptureMainWindowAsync(
            new ScreenshotOptions { CopyToClipboard = true, Delay = delay },
            cancellationToken);
    }
}
