using System.Runtime.InteropServices.WindowsRuntime;
using DevTools.Screenshot.Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DevTools.Screenshot.WinUi3.Sharp;

/// <summary>
/// <see cref="IScreenshot"/> implementation for WinUI 3 apps. Prefer the constructor taking a
/// window accessor; the parameterless one falls back to resolving the main window via reflection
/// (a public <c>MainWindow</c> property or the template's <c>m_window</c> field on <c>Application</c>).
/// </summary>
public sealed class WinUiScreenshot : IScreenshot
{
    private readonly Func<Window>? _windowAccessor;

    public WinUiScreenshot()
    {
    }

    public WinUiScreenshot(Func<Window> windowAccessor)
    {
        _windowAccessor = windowAccessor ?? throw new ArgumentNullException(nameof(windowAccessor));
    }

    public Task<ScreenshotResult> CaptureMainWindowAsync(
        ScreenshotOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var outputPath = options.RequireOutputPath();

        var window = _windowAccessor?.Invoke() ?? MainWindowResolver.Resolve();
        return window.DispatcherQueue.EnqueueAsync(() =>
            CaptureCoreAsync(window, outputPath, options.Delay, cancellationToken));
    }

    internal static async Task<ScreenshotResult> CaptureCoreAsync(
        Window window,
        string outputPath,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        if (window.Content is not FrameworkElement content)
            throw new InvalidOperationException("Main window has no content to capture.");

        await WaitForLayoutAndRenderAsync(content, cancellationToken);

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);

        var xamlRoot = content.XamlRoot
            ?? throw new InvalidOperationException("Main window content has no XamlRoot.");

        var scale = xamlRoot.RasterizationScale;
        if (content.ActualWidth < 1 || content.ActualHeight < 1)
            throw new InvalidOperationException("Window has no measurable area to capture.");

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(content);

        var pixels = await rtb.GetPixelsAsync();
        await SavePngAsync(outputPath, pixels, rtb.PixelWidth, rtb.PixelHeight, 96 * scale, cancellationToken);

        return new ScreenshotResult(Path.GetFullPath(outputPath), rtb.PixelWidth, rtb.PixelHeight);
    }

    private static async Task WaitForLayoutAndRenderAsync(FrameworkElement content, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (content.XamlRoot is not null)
            {
                content.UpdateLayout();
                await Task.Yield();

                if (content.ActualWidth >= 1 && content.ActualHeight >= 1)
                    return;
            }

            await Task.Delay(20, cancellationToken);
        }

        throw new InvalidOperationException("Timed out waiting for the main window to become ready for capture.");
    }

    private static async Task SavePngAsync(
        string outputPath,
        IBuffer pixels,
        int width,
        int height,
        double dpi,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(outputPath);
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream.AsRandomAccessStream());
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)width,
            (uint)height,
            dpi,
            dpi,
            pixels.ToArray());
        await encoder.FlushAsync();
    }
}
