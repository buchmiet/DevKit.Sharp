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
    #region Capture constants

    private const double BaseDpi = 96;
    private const double MinMeasurableDimension = 1;
    private const string PngFileExtension = ".png";
    private const string PngImageLabel = "PNG image";

    private const string NoXamlRootMessage = "Element has no XamlRoot.";
    private const string NoMeasurableAreaMessage = "Element has no measurable area to capture.";

    #endregion

    public static async Task<BitmapImage> CopyToClipboardAsync(FrameworkElement element)
    {
        var (_, _, pngBytes) = await CapturePngAsync(element);

        using var clipboardStream = new InMemoryRandomAccessStream();
        await clipboardStream.WriteAsync(pngBytes.AsBuffer());
        clipboardStream.Seek(0);

        var package = new DataPackage();
        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(clipboardStream));
        Clipboard.SetContent(package);

        return await CreateBitmapImageAsync(pngBytes);
    }

    public static async Task<(string Path, BitmapImage Preview)?> SaveToFileAsync(
        Window window,
        FrameworkElement element,
        string suggestedFileName)
    {
        var (_, _, pngBytes) = await CapturePngAsync(element);

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
            DefaultFileExtension = PngFileExtension,
        };
        picker.FileTypeChoices.Add(PngImageLabel, [PngFileExtension]);

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return null;

        await FileIO.WriteBytesAsync(file, pngBytes);

        return (file.Path, await CreateBitmapImageAsync(pngBytes));
    }

    private static async Task<(int Width, int Height, byte[] PngBytes)> CapturePngAsync(FrameworkElement element)
    {
        element.UpdateLayout();

        var xamlRoot = element.XamlRoot
            ?? throw new InvalidOperationException(NoXamlRootMessage);

        if (element.ActualWidth < MinMeasurableDimension || element.ActualHeight < MinMeasurableDimension)
            throw new InvalidOperationException(NoMeasurableAreaMessage);

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(element);

        var pixels = await rtb.GetPixelsAsync();
        var dpi = BaseDpi * xamlRoot.RasterizationScale;

        using var stream = new InMemoryRandomAccessStream();
        await EncodePngAsync(stream, pixels, rtb.PixelWidth, rtb.PixelHeight, dpi);

        stream.Seek(0);
        var pngBytes = new byte[stream.Size];
        await stream.ReadAsync(pngBytes.AsBuffer(), (uint)pngBytes.Length, InputStreamOptions.None);

        return (rtb.PixelWidth, rtb.PixelHeight, pngBytes);
    }

    private static async Task<BitmapImage> CreateBitmapImageAsync(byte[] pngBytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        stream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
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
