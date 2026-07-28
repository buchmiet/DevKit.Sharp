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
    #region Capture constants

    private const double BaseDpi = 96;
    private const double MinMeasurableDimension = 1;
    private const string PngExtension = "png";
    private const string SaveScreenshotTitle = "Save screenshot";

    private const string TopLevelUnavailableMessage = "Top level is not available.";
    private const string NoMeasurableAreaMessage = "Visual has no measurable area to capture.";
    private const string ClipboardUnavailableMessage = "Clipboard is not available.";

    #endregion

    public static async Task<Bitmap> CaptureAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException(TopLevelUnavailableMessage);

        await WaitForRenderAsync(visual);

        var bounds = visual.Bounds;
        if (bounds.Width < MinMeasurableDimension || bounds.Height < MinMeasurableDimension)
            throw new InvalidOperationException(NoMeasurableAreaMessage);

        var scale = topLevel.RenderScaling;
        var pixelSize = PixelSize.FromSize(bounds.Size, scale);
        var dpi = new Vector(BaseDpi * scale, BaseDpi * scale);

        var rendered = new RenderTargetBitmap(pixelSize, dpi);
        rendered.Render(visual);
        return rendered;
    }

    public static async Task<Bitmap> CopyToClipboardAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException(TopLevelUnavailableMessage);

        var clipboard = topLevel.Clipboard
            ?? throw new InvalidOperationException(ClipboardUnavailableMessage);

        using var rendered = await CaptureAsync(visual);
        var clipboardBitmap = CloneBitmap(rendered);
        await clipboard.SetValueAsync(DataFormat.Bitmap, clipboardBitmap);
        await clipboard.FlushAsync();

        return clipboardBitmap;
    }

    public static async Task<(string Path, Bitmap Preview)?> SaveToFileAsync(Visual visual, string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(visual)
            ?? throw new InvalidOperationException(TopLevelUnavailableMessage);

        var targetFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = SaveScreenshotTitle,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = PngExtension,
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
