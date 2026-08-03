using Windows.UI;

namespace DevKit.Screenshot.WinUi3.Sharp;

/// <summary>Options for <see cref="ElementScreenshotCapture"/> element captures.</summary>
public sealed record ElementCaptureOptions
{
    /// <summary>
    /// Color painted under the captured element when flattening transparency.
    /// When null, resolves the nearest ancestor with an opaque background,
    /// then the hosting window content root.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// When true (default), unpainted pixels are composited onto an opaque background
    /// so clipboard and file consumers that flatten alpha onto black still show content.
    /// </summary>
    public bool FlattenTransparency { get; init; } = true;
}
