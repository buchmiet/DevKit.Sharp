using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace DevKit.Screenshot.WinUi3.Sharp;

/// <summary>
/// Captures an arbitrary <see cref="FrameworkElement"/> to PNG bytes, a file, or the system clipboard.
/// Use <see cref="WinUiScreenshot"/> or <see cref="DevKit.Screenshot.Sharp.IScreenshot"/> for the main window.
/// </summary>
public static class ElementScreenshotCapture
{
    /// <summary>Renders <paramref name="element"/> and returns PNG bytes with the pixel dimensions.</summary>
    public static async Task<(int Width, int Height, byte[] PngBytes)> CapturePngAsync(
        FrameworkElement element,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);

        cancellationToken.ThrowIfCancellationRequested();

        element.UpdateLayout();

        var xamlRoot = element.XamlRoot
            ?? throw new InvalidOperationException("Element has no XamlRoot.");

        if (element.ActualWidth < 1 || element.ActualHeight < 1)
            throw new InvalidOperationException("Element has no measurable area to capture.");

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(element);

        cancellationToken.ThrowIfCancellationRequested();

        var pixels = await rtb.GetPixelsAsync();
        var dpi = 96 * xamlRoot.RasterizationScale;

        using var stream = new InMemoryRandomAccessStream();
        await PngEncoder.EncodeAsync(stream, pixels, rtb.PixelWidth, rtb.PixelHeight, dpi, cancellationToken);

        stream.Seek(0);
        var pngBytes = new byte[stream.Size];
        await stream.ReadAsync(pngBytes.AsBuffer(), (uint)pngBytes.Length, InputStreamOptions.None);

        return (rtb.PixelWidth, rtb.PixelHeight, pngBytes);
    }

    /// <summary>Renders <paramref name="element"/> and copies the result to the clipboard.</summary>
    public static async Task<(int Width, int Height)> CopyToClipboardAsync(
        FrameworkElement element,
        CancellationToken cancellationToken = default)
    {
        var (width, height, pngBytes) = await CapturePngAsync(element, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        using var clipboardStream = new InMemoryRandomAccessStream();
        await clipboardStream.WriteAsync(pngBytes.AsBuffer());
        clipboardStream.Seek(0);

        var package = new DataPackage();
        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(clipboardStream));
        Clipboard.SetContent(package);

        return (width, height);
    }

    /// <summary>Renders <paramref name="element"/> and writes a PNG after the user picks a path.</summary>
    public static async Task<(string Path, BitmapImage Preview)?> SaveToFileAsync(
        Window window,
        FrameworkElement element,
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(element);

        var (_, _, pngBytes) = await CapturePngAsync(element, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

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

        cancellationToken.ThrowIfCancellationRequested();

        await FileIO.WriteBytesAsync(file, pngBytes);

        using var previewStream = new InMemoryRandomAccessStream();
        await previewStream.WriteAsync(pngBytes.AsBuffer());
        previewStream.Seek(0);

        var preview = new BitmapImage();
        await preview.SetSourceAsync(previewStream);

        return (file.Path, preview);
    }
}
