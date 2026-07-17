namespace DevTools.Screenshot.Sharp;

public interface IScreenshot
{
    Task CaptureMainWindowAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);

    Task CaptureMainWindowAndExitAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);
}
