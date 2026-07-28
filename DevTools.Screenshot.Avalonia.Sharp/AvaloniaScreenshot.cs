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
    #region Capture constants

    private const double BaseDpi = 96;
    private const double MinMeasurableDimension = 1;

    private const string DesktopLifetimeRequiredMessage = "Classic desktop lifetime is required.";
    private const string MainWindowNullMessage = "MainWindow is null.";
    private const string NoMeasurableAreaMessage = "Window has no measurable area to capture.";
    private const string ClipboardUnavailableMessage = "System clipboard is not available.";

    #endregion

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
            throw new InvalidOperationException(DesktopLifetimeRequiredMessage);

        return desktop.MainWindow
            ?? throw new InvalidOperationException(MainWindowNullMessage);
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
        if (logicalSize.Width < MinMeasurableDimension || logicalSize.Height < MinMeasurableDimension)
            logicalSize = window.ClientSize;

        var pixelSize = PixelSize.FromSize(logicalSize, scale);
        if (pixelSize.Width < MinMeasurableDimension || pixelSize.Height < MinMeasurableDimension)
            throw new InvalidOperationException(NoMeasurableAreaMessage);

        var dpi = new Vector(BaseDpi * scale, BaseDpi * scale);
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
            ?? throw new InvalidOperationException(ClipboardUnavailableMessage);

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
