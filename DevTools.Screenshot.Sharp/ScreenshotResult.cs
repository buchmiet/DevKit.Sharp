namespace DevTools.Screenshot.Sharp;

/// <summary>Outcome of a successful capture.</summary>
/// <param name="OutputPath">Full path of the written PNG file.</param>
/// <param name="PixelWidth">Width of the captured bitmap in physical pixels.</param>
/// <param name="PixelHeight">Height of the captured bitmap in physical pixels.</param>
public readonly record struct ScreenshotResult(string OutputPath, int PixelWidth, int PixelHeight);
