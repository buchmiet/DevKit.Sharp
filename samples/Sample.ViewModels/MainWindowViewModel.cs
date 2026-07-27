using DevTools.Screenshot.Sharp;

namespace Sample.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly ScreenshotOptions _options;

    public MainWindowViewModel(ScreenshotOptions options)
    {
        _options = options;
    }

    public string WindowTitle => "DevTools.Screenshot Sample";

    public string StatusText =>
        _options.IsEnabled
            ? _options.CopyToClipboard
                ? "Screenshot target: system clipboard"
                : $"Screenshot target: {_options.OutputPath}"
            : $"Run with {ScreenshotArgs.PathSwitch} <path> or {ScreenshotArgs.ClipboardSwitch} to capture on load.";

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
