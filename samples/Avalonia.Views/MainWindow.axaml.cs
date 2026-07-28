using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DevTools.Screenshot.Sharp;
using Sample.ViewModels;

namespace Avalonia.Views;

public partial class MainWindow : Window
{
    #region File picker and status messages

    private const string SaveScreenshotTitle = "Save screenshot";
    private const string MainWindowFileName = "main-window.png";
    private const string PngExtension = "png";

    private const string CopiedPanelStatusFormat = "Copied {0} panel {1} x {2} px to the clipboard.";
    private const string PanelCaptureFailedFormat = "Panel capture failed: {0}";
    private const string SavedPanelStatusFormat = "Saved {0} panel to {1}.";
    private const string PanelSaveFailedFormat = "Panel save failed: {0}";
    private const string CopiedMainWindowStatusFormat = "Copied main window {0} x {1} px to the clipboard.";
    private const string MainWindowCaptureFailedFormat = "Main-window capture failed: {0}";
    private const string SavedMainWindowStatusFormat = "Saved main window to {0}.";
    private const string MainWindowSaveFailedFormat = "Main-window save failed: {0}";
    private const string ClipboardUnavailableMessage = "Clipboard is not available.";
    private const string ClipboardMissingBitmapMessage = "Clipboard does not contain a bitmap image.";
    private const string PastedClipboardStatusFormat = "Pasted {0} x {1} px from the clipboard.";

    #endregion

    private readonly IScreenshot _screenshot;
    private Bitmap? _previewBitmap;

    public MainWindow(MainWindowViewModel viewModel, IScreenshot screenshot)
    {
        _screenshot = screenshot;
        InitializeComponent();
        DataContext = viewModel;
        SamplePanelBuilder.Populate(PanelsHost, viewModel.Panels, CopyPanelAsync, SavePanelAsync);
    }

    private async Task CopyPanelAsync(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var bitmap = await VisualScreenshotCapture.CopyToClipboardAsync(panelSurface);
            ShowPreview(bitmap, string.Format(CopiedPanelStatusFormat, panel.Title, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(PanelCaptureFailedFormat, ex.Message));
        }
    }

    private async Task SavePanelAsync(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var saved = await VisualScreenshotCapture.SaveToFileAsync(panelSurface, panel.SuggestedFileName);
            if (saved is null)
                return;

            ShowPreview(saved.Value.Preview, string.Format(SavedPanelStatusFormat, panel.Title, saved.Value.Path));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(PanelSaveFailedFormat, ex.Message));
        }
    }

    private async void OnCopyMainWindowClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _screenshot.CaptureMainWindowToClipboardAsync();
            await RefreshClipboardPreviewAsync();
            SetClipboardPreviewStatus(
                string.Format(CopiedMainWindowStatusFormat, result.PixelWidth, result.PixelHeight));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(MainWindowCaptureFailedFormat, ex.Message));
        }
    }

    private async void OnSaveMainWindowClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = SaveScreenshotTitle,
                SuggestedFileName = MainWindowFileName,
                DefaultExtension = PngExtension,
                FileTypeChoices = [FilePickerFileTypes.ImagePng],
            });

            if (targetFile is null)
                return;

            var result = await _screenshot.CaptureMainWindowAsync(targetFile.TryGetLocalPath() ?? targetFile.Path.LocalPath);
            await using var stream = await targetFile.OpenReadAsync();
            using var bitmap = new Bitmap(stream);
            ShowPreview(VisualScreenshotCapture.CloneBitmap(bitmap), string.Format(SavedMainWindowStatusFormat, result.OutputPath));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(MainWindowSaveFailedFormat, ex.Message));
        }
    }

    private async void OnPasteFromClipboardClick(object? sender, RoutedEventArgs e) =>
        await RefreshClipboardPreviewAsync();

    private async Task RefreshClipboardPreviewAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            SetClipboardPreviewStatus(ClipboardUnavailableMessage);
            ClipboardPreviewImage.Source = null;
            return;
        }

        using var data = await clipboard.TryGetDataAsync();
        var bitmap = data is null ? null : await data.TryGetValueAsync(DataFormat.Bitmap);
        if (bitmap is null)
        {
            SetClipboardPreviewStatus(ClipboardMissingBitmapMessage);
            ClipboardPreviewImage.Source = null;
            return;
        }

        ShowPreview(bitmap, string.Format(PastedClipboardStatusFormat, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
    }

    private void ShowPreview(Bitmap bitmap, string status)
    {
        _previewBitmap?.Dispose();
        _previewBitmap = VisualScreenshotCapture.CloneBitmap(bitmap);
        ClipboardPreviewImage.Source = _previewBitmap;
        SetClipboardPreviewStatus(status);
    }

    private void SetClipboardPreviewStatus(string message) =>
        ClipboardPreviewStatus.Text = message;
}
