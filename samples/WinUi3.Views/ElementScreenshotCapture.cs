using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace WinUi3.Views;

public static class ElementScreenshotCapture
{
    public static async Task<(int Width, int Height, byte[] PngBytes)> CapturePngAsync(FrameworkElement element)
    {
        element.UpdateLayout();

        var xamlRoot = element.XamlRoot
            ?? throw new InvalidOperationException("Element has no XamlRoot.");

        if (element.ActualWidth < 1 || element.ActualHeight < 1)
            throw new InvalidOperationException("Element has no measurable area to capture.");

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(element);

        var pixels = await rtb.GetPixelsAsync();
        var dpi = 96 * xamlRoot.RasterizationScale;

        using var stream = new InMemoryRandomAccessStream();
        await EncodePngAsync(stream, pixels, rtb.PixelWidth, rtb.PixelHeight, dpi);

        stream.Seek(0);
        var pngBytes = new byte[stream.Size];
        await stream.ReadAsync(pngBytes.AsBuffer(), (uint)pngBytes.Length, InputStreamOptions.None);

        return (rtb.PixelWidth, rtb.PixelHeight, pngBytes);
    }

    public static async Task<(int Width, int Height)> CopyToClipboardAsync(FrameworkElement element)
    {
        var (width, height, pngBytes) = await CapturePngAsync(element);

        using var clipboardStream = new InMemoryRandomAccessStream();
        await clipboardStream.WriteAsync(pngBytes.AsBuffer());
        clipboardStream.Seek(0);

        var package = new DataPackage();
        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(clipboardStream));
        Clipboard.SetContent(package);

        return (width, height);
    }

    public static async Task<(string Path, BitmapImage Preview)?> SaveToFileAsync(
        Window window,
        FrameworkElement element,
        string suggestedFileName)
    {
        var (width, height, pngBytes) = await CapturePngAsync(element);

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
            DefaultFileExtension = ".png",
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return null;

        await FileIO.WriteBytesAsync(file, pngBytes);

        using var previewStream = new InMemoryRandomAccessStream();
        await previewStream.WriteAsync(pngBytes.AsBuffer());
        previewStream.Seek(0);

        var preview = new BitmapImage();
        await preview.SetSourceAsync(previewStream);

        return (file.Path, preview);
    }

    private static async Task EncodePngAsync(
        IRandomAccessStream stream,
        IBuffer pixels,
        int width,
        int height,
        double dpi)
    {
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
