using Avalonia.Media;

namespace DevKit.Screenshot.Avalonia.Sharp;

/// <summary>Options for <see cref="VisualScreenshotCapture"/> subtree captures.</summary>
public sealed record VisualCaptureOptions
{
    /// <summary>
    /// Brush painted under the captured visual when flattening transparency.
    /// When null, resolves the nearest visual ancestor with an opaque background,
    /// then the hosting <c>TopLevel</c> background brush.
    /// </summary>
    public IBrush? Background { get; init; }

    /// <summary>
    /// When true (default), unpainted pixels are composited onto an opaque background
    /// so clipboard and file consumers that flatten alpha onto black still show content.
    /// </summary>
    public bool FlattenTransparency { get; init; } = true;
}
