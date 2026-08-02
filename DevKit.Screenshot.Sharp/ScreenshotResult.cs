namespace DevKit.Screenshot.Sharp;

/// <summary>Outcome of a successful capture.</summary>
/// <param name="OutputPath">Full path of the written PNG file, or null when copied to the clipboard.</param>
/// <param name="PixelWidth">Width of the captured bitmap in physical pixels.</param>
/// <param name="PixelHeight">Height of the captured bitmap in physical pixels.</param>
/// <param name="CopiedToClipboard">True when the capture was placed on the system clipboard.</param>
public readonly record struct ScreenshotResult(
    string? OutputPath,
    int PixelWidth,
    int PixelHeight,
    bool CopiedToClipboard = false);
