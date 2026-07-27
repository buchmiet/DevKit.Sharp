namespace DevTools.Screenshot.Sharp;

/// <summary>Options controlling a main-window screenshot capture.</summary>
public sealed class ScreenshotOptions
{
    /// <summary>Path of the PNG file to write. Ignored when <see cref="CopyToClipboard"/> is true.</summary>
    public string? OutputPath { get; init; }

    /// <summary>When true, places the capture on the system clipboard instead of writing a file.</summary>
    public bool CopyToClipboard { get; init; }

    /// <summary>
    /// Extra settle time applied after the window has laid out and rendered its first frame,
    /// immediately before the capture. Defaults to <see cref="DefaultDelay"/>.
    /// </summary>
    public TimeSpan Delay { get; init; } = DefaultDelay;

    /// <summary>
    /// When used with an <c>AttachScreenshot</c> hook: close the app after a successful capture,
    /// or exit with <see cref="ScreenshotExitCodes.CaptureFailed"/> when the capture fails.
    /// </summary>
    public bool ExitAfterCapture { get; init; }

    /// <summary>True when a file path or clipboard destination is configured.</summary>
    public bool IsEnabled => CopyToClipboard || !string.IsNullOrWhiteSpace(OutputPath);

    /// <summary>Default settle delay before capture (150 ms).</summary>
    public static TimeSpan DefaultDelay { get; } = TimeSpan.FromMilliseconds(150);

    /// <summary>A disabled options instance (no destination).</summary>
    public static ScreenshotOptions Disabled { get; } = new();

    /// <summary>Throws when <see cref="IsEnabled"/> is false.</summary>
    public void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                $"{nameof(ScreenshotOptions)} must specify {nameof(OutputPath)} or {nameof(CopyToClipboard)} to capture a screenshot.");
        }
    }

    /// <summary>Returns <see cref="OutputPath"/> or throws when a file destination is required.</summary>
    public string RequireOutputPath() =>
        !string.IsNullOrWhiteSpace(OutputPath)
            ? OutputPath!
            : throw new InvalidOperationException(
                $"{nameof(ScreenshotOptions)}.{nameof(OutputPath)} is required to capture a screenshot to a file.");
}
