namespace Sample.ViewModels;

public sealed class ScreenshotOptions
{
    public string? OutputPath { get; init; }

    public bool ExitAfterCapture { get; init; }

    public int DelayMs { get; init; } = 150;
}
