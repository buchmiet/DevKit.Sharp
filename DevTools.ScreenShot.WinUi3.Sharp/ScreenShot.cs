using System.Runtime.InteropServices.WindowsRuntime;
using DevTools.ScreenShot.Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DevTools.ScreenShot.WinUi3.Sharp;

public sealed class ScreenShot : IScreenShot
{
    public Task CaptureMainWindowAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default)
    {
        var window = MainWindowResolver.Resolve();
        return window.DispatcherQueue.EnqueueAsync(() =>
            CaptureMainWindowCoreAsync(window, outputPath, delayMs, shutdownAfterCapture: false, cancellationToken));
    }

    public Task CaptureMainWindowAndExitAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default)
    {
        var window = MainWindowResolver.Resolve();
        return window.DispatcherQueue.EnqueueAsync(() =>
            CaptureMainWindowCoreAsync(window, outputPath, delayMs, shutdownAfterCapture: true, cancellationToken));
    }

    private static async Task CaptureMainWindowCoreAsync(
        Window window,
        string outputPath,
        int delayMs,
        bool shutdownAfterCapture,
        CancellationToken cancellationToken)
    {
        if (window.Content is not FrameworkElement content)
            throw new InvalidOperationException("Main window has no content to capture.");

        await WaitForLayoutAndRenderAsync(content, cancellationToken);

        if (delayMs > 0)
            await Task.Delay(delayMs, cancellationToken);

        var xamlRoot = content.XamlRoot
            ?? throw new InvalidOperationException("Main window content has no XamlRoot.");

        var scale = xamlRoot.RasterizationScale;
        if (content.ActualWidth < 1 || content.ActualHeight < 1)
            throw new InvalidOperationException("Window has no measurable area to capture.");

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(content);

        var pixels = await rtb.GetPixelsAsync();
        await SavePngAsync(outputPath, pixels, rtb.PixelWidth, rtb.PixelHeight, 96 * scale, cancellationToken);

        if (shutdownAfterCapture)
            Application.Current?.Exit();
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

        using var stream = File.OpenWrite(outputPath);
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
