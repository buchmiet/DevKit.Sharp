using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DevKit.Screenshot.WinUi3.Sharp;

internal static class PngEncoder
{
    internal static async Task EncodeAsync(
        IRandomAccessStream stream,
        IBuffer pixels,
        int width,
        int height,
        double dpi,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
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
