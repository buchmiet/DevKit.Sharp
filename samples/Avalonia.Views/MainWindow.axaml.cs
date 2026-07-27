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
            ShowPreview(bitmap, $"Copied {panel.Title} panel {bitmap.PixelSize.Width} x {bitmap.PixelSize.Height} px to the clipboard.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Panel capture failed: {ex.Message}");
        }
    }

    private async Task SavePanelAsync(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var saved = await VisualScreenshotCapture.SaveToFileAsync(panelSurface, panel.SuggestedFileName);
            if (saved is null)
                return;

            ShowPreview(saved.Value.Preview, $"Saved {panel.Title} panel to {saved.Value.Path}.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Panel save failed: {ex.Message}");
        }
    }

    private async void OnCopyMainWindowClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _screenshot.CaptureMainWindowToClipboardAsync();
            await RefreshClipboardPreviewAsync();
            SetClipboardPreviewStatus(
                $"Copied main window {result.PixelWidth} x {result.PixelHeight} px to the clipboard.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Main-window capture failed: {ex.Message}");
        }
    }

    private async void OnSaveMainWindowClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save screenshot",
                SuggestedFileName = "main-window.png",
                DefaultExtension = "png",
                FileTypeChoices = [FilePickerFileTypes.ImagePng],
            });

            if (targetFile is null)
                return;

            var result = await _screenshot.CaptureMainWindowAsync(targetFile.TryGetLocalPath() ?? targetFile.Path.LocalPath);
            await using var stream = await targetFile.OpenReadAsync();
            using var bitmap = new Bitmap(stream);
            ShowPreview(VisualScreenshotCapture.CloneBitmap(bitmap), $"Saved main window to {result.OutputPath}.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Main-window save failed: {ex.Message}");
        }
    }

    private async void OnPasteFromClipboardClick(object? sender, RoutedEventArgs e) =>
        await RefreshClipboardPreviewAsync();

    private async Task RefreshClipboardPreviewAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            SetClipboardPreviewStatus("Clipboard is not available.");
            ClipboardPreviewImage.Source = null;
            return;
        }

        using var data = await clipboard.TryGetDataAsync();
        var bitmap = data is null ? null : await data.TryGetValueAsync(DataFormat.Bitmap);
        if (bitmap is null)
        {
            SetClipboardPreviewStatus("Clipboard does not contain a bitmap image.");
            ClipboardPreviewImage.Source = null;
            return;
        }

        ShowPreview(bitmap, $"Pasted {bitmap.PixelSize.Width} x {bitmap.PixelSize.Height} px from the clipboard.");
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
