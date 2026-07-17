using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DevTools.Screenshot.Sharp;

namespace DevTools.Screenshot.Avalonia.Sharp;

/// <summary><see cref="IScreenshot"/> implementation for Avalonia desktop apps.</summary>
public sealed class AvaloniaScreenshot : IScreenshot
{
    public Task<ScreenshotResult> CaptureMainWindowAsync(
        ScreenshotOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var outputPath = options.RequireOutputPath();

        return Dispatcher.UIThread.InvokeAsync(() =>
            CaptureCoreAsync(ResolveMainWindow(), outputPath, options.Delay, cancellationToken));
    }

    internal static Window ResolveMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Classic desktop lifetime is required.");

        return desktop.MainWindow
            ?? throw new InvalidOperationException("MainWindow is null.");
    }

    internal static async Task<ScreenshotResult> CaptureCoreAsync(
        Window window,
        string outputPath,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        if (!window.IsVisible)
            window.Show();

        await WaitForLayoutAndRenderAsync(cancellationToken);

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);

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

        return new ScreenshotResult(Path.GetFullPath(outputPath), pixelSize.Width, pixelSize.Height);
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
