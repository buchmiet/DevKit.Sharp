namespace DevTools.Screenshot.Sharp;

/// <summary>Options controlling a main-window screenshot capture.</summary>
public sealed class ScreenshotOptions
{
    /// <summary>Path of the PNG file to write. When null or whitespace the capture is disabled.</summary>
    public string? OutputPath { get; init; }

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

    /// <summary>True when <see cref="OutputPath"/> is set.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(OutputPath);

    /// <summary>Default settle delay before capture (150 ms).</summary>
    public static TimeSpan DefaultDelay { get; } = TimeSpan.FromMilliseconds(150);

    /// <summary>A disabled options instance (no output path).</summary>
    public static ScreenshotOptions Disabled { get; } = new();

    /// <summary>Returns <see cref="OutputPath"/> or throws when the options are disabled.</summary>
    public string RequireOutputPath() =>
        IsEnabled
            ? OutputPath!
            : throw new InvalidOperationException(
                $"{nameof(ScreenshotOptions)}.{nameof(OutputPath)} is required to capture a screenshot.");
}
