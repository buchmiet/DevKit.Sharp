using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DevTools.Screenshot.Sharp;

namespace DevTools.Screenshot.Avalonia.Sharp;

public sealed class AvaloniaScreenshot : IScreenshot
{
    public Task CaptureMainWindowAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
            CaptureMainWindowCoreAsync(outputPath, delayMs, shutdownAfterCapture: false, cancellationToken));
    }

    public Task CaptureMainWindowAndExitAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
            CaptureMainWindowCoreAsync(outputPath, delayMs, shutdownAfterCapture: true, cancellationToken));
    }

    private static async Task CaptureMainWindowCoreAsync(
        string outputPath,
        int delayMs,
        bool shutdownAfterCapture,
        CancellationToken cancellationToken)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Classic desktop lifetime is required.");

        var window = desktop.MainWindow
            ?? throw new InvalidOperationException("MainWindow is null.");

        if (!window.IsVisible)
            window.Show();

        await WaitForLayoutAndRenderAsync(cancellationToken);

        if (delayMs > 0)
            await Task.Delay(delayMs, cancellationToken);

        var scale = window.RenderScaling;
        var logicalSize = window.Bounds.Size;
        if (logicalSize.Width < 1 || logicalSize.Height < 1)
            logicalSize = window.ClientSize;

        var pixelSize = PixelSize.FromSize(logicalSize, scale);
        if (pixelSize.Width < 1 || pixelSize.Height < 1)
            throw new InvalidOperationException("Window has no measurable area to capture.");

        var dpi = new Vector(96 * scale, 96 * scale);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var bitmap = new RenderTargetBitmap(pixelSize, dpi);
        bitmap.Render(window);
        bitmap.Save(outputPath);

        if (shutdownAfterCapture)
            desktop.Shutdown();
    }

    private static async Task WaitForLayoutAndRenderAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dispatcher.UIThread.RunJobs();

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded, cancellationToken);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render, cancellationToken);
        Dispatcher.UIThread.RunJobs();
    }
}
