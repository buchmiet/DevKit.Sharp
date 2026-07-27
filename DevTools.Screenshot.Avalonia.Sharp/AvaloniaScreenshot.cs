using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
        options.EnsureEnabled();

        return Dispatcher.UIThread.InvokeAsync(() =>
            CaptureCoreAsync(ResolveMainWindow(), options, cancellationToken));
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
        ScreenshotOptions options,
        CancellationToken cancellationToken)
    {
        if (!window.IsVisible)
            window.Show();

        await WaitForLayoutAndRenderAsync(cancellationToken);

        if (options.Delay > TimeSpan.Zero)
            await Task.Delay(options.Delay, cancellationToken);

        var scale = window.RenderScaling;
        var logicalSize = window.Bounds.Size;
        if (logicalSize.Width < 1 || logicalSize.Height < 1)
            logicalSize = window.ClientSize;

        var pixelSize = PixelSize.FromSize(logicalSize, scale);
        if (pixelSize.Width < 1 || pixelSize.Height < 1)
            throw new InvalidOperationException("Window has no measurable area to capture.");

        var dpi = new Vector(96 * scale, 96 * scale);
        using var rendered = new RenderTargetBitmap(pixelSize, dpi);
        rendered.Render(window);

        if (options.CopyToClipboard)
        {
            await CopyToClipboardAsync(window, rendered);
            return new ScreenshotResult(null, pixelSize.Width, pixelSize.Height, CopiedToClipboard: true);
        }

        var outputPath = options.RequireOutputPath();
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        rendered.Save(outputPath);
        return new ScreenshotResult(Path.GetFullPath(outputPath), pixelSize.Width, pixelSize.Height);
    }

    private static async Task CopyToClipboardAsync(Window window, RenderTargetBitmap rendered)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard
            ?? throw new InvalidOperationException("System clipboard is not available.");

        using var pngStream = new MemoryStream();
        rendered.Save(pngStream);
        pngStream.Position = 0;
        var clipboardBitmap = new Bitmap(pngStream);

        await clipboard.SetValueAsync(DataFormat.Bitmap, clipboardBitmap);
        await clipboard.FlushAsync();
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
