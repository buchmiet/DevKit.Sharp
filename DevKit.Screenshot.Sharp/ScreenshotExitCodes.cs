namespace DevKit.Screenshot.Sharp;

/// <summary>Process exit code conventions used by the <c>AttachScreenshot</c> CI hooks.</summary>
public static class ScreenshotExitCodes
{
    /// <summary>
    /// Exit code used when a capture requested with <see cref="ScreenshotOptions.ExitAfterCapture"/>
    /// fails (mirrors BSD's <c>EX_SOFTWARE</c>), so CI can distinguish "no screenshot" from success.
    /// </summary>
    public const int CaptureFailed = 70;
}
