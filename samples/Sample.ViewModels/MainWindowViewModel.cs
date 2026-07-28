using DevTools.Screenshot.Sharp;

namespace Sample.ViewModels;

public sealed class MainWindowViewModel
{
    #region UI copy

    private const string WindowTitleText = "DevTools.Screenshot Sample";
    private const string ClipboardTargetStatus = "Screenshot target: system clipboard";
    private const string FileTargetStatusFormat = "Screenshot target: {0}";
    private const string DisabledStatusFormat = "Run with {0} <path> or {1} to capture on load.";

    #endregion

    private readonly ScreenshotOptions _options;

    public MainWindowViewModel(ScreenshotOptions options)
    {
        _options = options;
    }

    public string WindowTitle => WindowTitleText;

    public string StatusText =>
        _options.IsEnabled
            ? _options.CopyToClipboard
                ? ClipboardTargetStatus
                : string.Format(FileTargetStatusFormat, _options.OutputPath)
            : string.Format(DisabledStatusFormat, ScreenshotArgs.PathSwitch, ScreenshotArgs.ClipboardSwitch);

    public IReadOnlyList<ColorPanelViewModel> Panels { get; } =
    [
        new("Coral", "#FF6B6B"),
        new("Mint", "#4ECDC4"),
        new("Sun", "#FFE66D"),
        new("Sky", "#4D96FF"),
        new("Grape", "#9B5DE5"),
        new("Peach", "#F15BB5"),
    ];
}
