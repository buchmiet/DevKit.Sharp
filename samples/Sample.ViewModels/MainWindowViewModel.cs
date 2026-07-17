using DevTools.ScreenShot.Sharp;

namespace Sample.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly IScreenShot _screenshot;
    private readonly ScreenshotOptions _options;

    public MainWindowViewModel(IScreenShot screenshot, ScreenshotOptions options)
    {
        _screenshot = screenshot;
        _options = options;
    }

    public string WindowTitle => "DevTools.ScreenShot Sample";

    public string StatusText =>
        string.IsNullOrWhiteSpace(_options.OutputPath)
            ? "Run with --screenshot <path> to capture on load."
            : $"Screenshot target: {_options.OutputPath}";

    public IReadOnlyList<ColorPanelViewModel> Panels { get; } =
    [
        new("Coral", "#FF6B6B"),
        new("Mint", "#4ECDC4"),
        new("Sun", "#FFE66D"),
        new("Sky", "#4D96FF"),
        new("Grape", "#9B5DE5"),
        new("Peach", "#F15BB5"),
    ];

    public Task OnLoadedAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.OutputPath))
            return Task.CompletedTask;

        return _options.ExitAfterCapture
            ? _screenshot.CaptureMainWindowAndExitAsync(_options.OutputPath, _options.DelayMs)
            : _screenshot.CaptureMainWindowAsync(_options.OutputPath, _options.DelayMs);
    }
}
