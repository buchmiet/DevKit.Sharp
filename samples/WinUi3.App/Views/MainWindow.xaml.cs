using DevTools.Screenshot.Sharp;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Sample.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace WinUi3.Views;

public sealed partial class MainWindow : Window
{
    #region Window and file picker constants

    private const int WindowWidth = 1100;
    private const int WindowHeight = 720;
    private const string MainWindowFileName = "main-window.png";
    private const string PngFileExtension = ".png";
    private const string PngImageLabel = "PNG image";

    private const string CopiedPanelStatusFormat = "Copied {0} panel {1} x {2} px to the clipboard.";
    private const string PanelCaptureFailedFormat = "Panel capture failed: {0}";
    private const string SavedPanelStatusFormat = "Saved {0} panel to {1}.";
    private const string PanelSaveFailedFormat = "Panel save failed: {0}";
    private const string CopiedMainWindowStatusFormat = "Copied main window {0} x {1} px to the clipboard.";
    private const string MainWindowCaptureFailedFormat = "Main-window capture failed: {0}";
    private const string SavedMainWindowStatusFormat = "Saved main window to {0}.";
    private const string MainWindowSaveFailedFormat = "Main-window save failed: {0}";
    private const string ClipboardMissingBitmapMessage = "Clipboard does not contain a bitmap image.";
    private const string PastedClipboardStatusFormat = "Pasted {0} x {1} px from the clipboard.";

    #endregion

    private readonly IScreenshot _screenshot;

    public MainWindow(MainWindowViewModel viewModel, IScreenshot screenshot)
    {
        _screenshot = screenshot;
        InitializeComponent();
        Title = viewModel.WindowTitle;
        StatusText.Text = viewModel.StatusText;
        var appWindow = AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this)));
        appWindow.Resize(new Windows.Graphics.SizeInt32(WindowWidth, WindowHeight));
        SamplePanelBuilder.Populate(PanelsHost, viewModel.Panels, CopyPanelAsync, SavePanelAsync);
    }

    private async Task CopyPanelAsync(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var preview = await ElementScreenshotCapture.CopyToClipboardAsync(panelSurface);
            ShowPreview(preview, string.Format(
                CopiedPanelStatusFormat,
                panel.Title,
                preview.PixelWidth,
                preview.PixelHeight));
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
            var saved = await ElementScreenshotCapture.SaveToFileAsync(this, panelSurface, panel.SuggestedFileName);
            if (saved is null)
                return;

            ShowPreview(saved.Value.Preview, string.Format(SavedPanelStatusFormat, panel.Title, saved.Value.Path));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(PanelSaveFailedFormat, ex.Message));
        }
    }

    private async void OnCopyMainWindowClick(object sender, RoutedEventArgs e)
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

    private async void OnSaveMainWindowClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = Path.GetFileNameWithoutExtension(MainWindowFileName),
                DefaultFileExtension = PngFileExtension,
            };
            picker.FileTypeChoices.Add(PngImageLabel, [PngFileExtension]);

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            var result = await _screenshot.CaptureMainWindowAsync(file.Path);
            var bytes = await FileIO.ReadBufferAsync(file);
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes);
            stream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            ShowPreview(bitmap, string.Format(SavedMainWindowStatusFormat, result.OutputPath));
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus(string.Format(MainWindowSaveFailedFormat, ex.Message));
        }
    }

    private async void OnPasteFromClipboardClick(object sender, RoutedEventArgs e) =>
        await RefreshClipboardPreviewAsync();

    private async Task RefreshClipboardPreviewAsync()
    {
        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap))
        {
            SetClipboardPreviewStatus(ClipboardMissingBitmapMessage);
            ClipboardPreviewImage.Source = null;
            return;
        }

        var streamReference = await content.GetBitmapAsync();
        using IRandomAccessStreamWithContentType stream = await streamReference.OpenReadAsync();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);

        ShowPreview(bitmap, string.Format(PastedClipboardStatusFormat, bitmap.PixelWidth, bitmap.PixelHeight));
    }

    private void ShowPreview(BitmapImage bitmap, string status)
    {
        ClipboardPreviewImage.Source = bitmap;
        SetClipboardPreviewStatus(status);
    }

    private void SetClipboardPreviewStatus(string message) =>
        ClipboardPreviewStatus.Text = message;
}
