namespace DevTools.ScreenShot.Sharp;

public interface IScreenShot
{
    Task CaptureMainWindowAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);

    Task CaptureMainWindowAndExitAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);
}
