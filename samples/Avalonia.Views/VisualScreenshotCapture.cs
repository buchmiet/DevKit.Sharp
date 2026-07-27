using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Avalonia.Views;

internal static class VisualScreenshotCapture
{
    public static async Task<Bitmap> CaptureAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException("Top level is not available.");

        await WaitForRenderAsync(visual);

        var bounds = visual.Bounds;
        if (bounds.Width < 1 || bounds.Height < 1)
            throw new InvalidOperationException("Visual has no measurable area to capture.");

        var scale = topLevel.RenderScaling;
        var pixelSize = PixelSize.FromSize(bounds.Size, scale);
        var dpi = new Vector(96 * scale, 96 * scale);

        var rendered = new RenderTargetBitmap(pixelSize, dpi);
        rendered.Render(visual);
        return rendered;
    }

    public static async Task<Bitmap> CopyToClipboardAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException("Top level is not available.");

        var clipboard = topLevel.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available.");

        using var rendered = await CaptureAsync(visual);
        var clipboardBitmap = CloneBitmap(rendered);
        await clipboard.SetValueAsync(DataFormat.Bitmap, clipboardBitmap);
        await clipboard.FlushAsync();

        return clipboardBitmap;
    }

    public static async Task<(string Path, Bitmap Preview)?> SaveToFileAsync(Visual visual, string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException("Top level is not available.");

        var targetFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save screenshot",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "png",
            FileTypeChoices =
            [
                FilePickerFileTypes.ImagePng,
            ],
        });

        if (targetFile is null)
            return null;

        using var rendered = await CaptureAsync(visual);
        await using var stream = await targetFile.OpenWriteAsync();
        rendered.Save(stream);

        var path = targetFile.TryGetLocalPath() ?? targetFile.Name;
        return (path, CloneBitmap(rendered));
    }

    public static Bitmap CloneBitmap(Bitmap source)
    {
        using var stream = new MemoryStream();
        source.Save(stream);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    private static async Task WaitForRenderAsync(Visual visual)
    {
        if (visual is Layoutable layoutable)
            layoutable.UpdateLayout();

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        Dispatcher.UIThread.RunJobs();
    }
}
