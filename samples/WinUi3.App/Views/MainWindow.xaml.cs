using DevKit.Screenshot.Sharp;
using DevKit.Screenshot.WinUi3.Sharp;
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
    private readonly IScreenshot _screenshot;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, IScreenshot screenshot)
    {
        ViewModel = viewModel;
        _screenshot = screenshot;
        InitializeComponent();
        Title = viewModel.WindowTitle;
        StatusText.Text = viewModel.StatusText;
        var appWindow = AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this)));
        appWindow.Resize(new Windows.Graphics.SizeInt32(1100, 720));
        SamplePanelBuilder.Populate(PanelsGrid, viewModel.Panels, OnCopyPanel, OnSavePanel);
    }

    private async void OnCopyPanel(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var (width, height) = await ElementScreenshotCapture.CopyToClipboardAsync(panelSurface);
            SetClipboardPreviewStatus($"Copied {panel.Title} panel {width} x {height} px to the clipboard.");
            await RefreshClipboardPreviewAsync();
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Panel capture failed: {ex.Message}");
        }
    }

    private async void OnSavePanel(Border panelSurface, ColorPanelViewModel panel)
    {
        try
        {
            var saved = await ElementScreenshotCapture.SaveToFileAsync(this, panelSurface, panel.SuggestedFileName);
            if (saved is null)
                return;

            ClipboardPreviewImage.Source = saved.Value.Preview;
            SetClipboardPreviewStatus($"Saved {panel.Title} panel to {saved.Value.Path}.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Panel save failed: {ex.Message}");
        }
    }

    private async void OnCopyMainWindowClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _screenshot.CaptureMainWindowToClipboardAsync();
            SetClipboardPreviewStatus(
                $"Copied main window {result.PixelWidth} x {result.PixelHeight} px to the clipboard.");
            await RefreshClipboardPreviewAsync();
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Main-window capture failed: {ex.Message}");
        }
    }

    private async void OnSaveMainWindowClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = "main-window",
                DefaultFileExtension = ".png",
            };
            picker.FileTypeChoices.Add("PNG image", [".png"]);

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

            var preview = new BitmapImage();
            await preview.SetSourceAsync(stream);
            ClipboardPreviewImage.Source = preview;
            SetClipboardPreviewStatus($"Saved main window to {result.OutputPath}.");
        }
        catch (Exception ex)
        {
            SetClipboardPreviewStatus($"Main-window save failed: {ex.Message}");
        }
    }

    private async void OnPasteFromClipboardClick(object sender, RoutedEventArgs e) =>
        await RefreshClipboardPreviewAsync();

    private async Task RefreshClipboardPreviewAsync()
    {
        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap))
        {
            SetClipboardPreviewStatus("Clipboard does not contain a bitmap image.");
            ClipboardPreviewImage.Source = null;
            return;
        }

        var streamReference = await content.GetBitmapAsync();
        using IRandomAccessStreamWithContentType stream = await streamReference.OpenReadAsync();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);

        ClipboardPreviewImage.Source = bitmap;
        SetClipboardPreviewStatus($"Pasted {bitmap.PixelWidth} x {bitmap.PixelHeight} px from the clipboard.");
    }

    private void SetClipboardPreviewStatus(string message) =>
        ClipboardPreviewStatus.Text = message;
}
