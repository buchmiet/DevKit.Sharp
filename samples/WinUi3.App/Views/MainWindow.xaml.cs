using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sample.ViewModels;
using WinRT.Interop;

namespace WinUi3.Views;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = viewModel.WindowTitle;
        StatusText.Text = viewModel.StatusText;
        var appWindow = AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this)));
        appWindow.Resize(new Windows.Graphics.SizeInt32(900, 560));
        SamplePanelBuilder.Populate(PanelsGrid, viewModel.Panels);
    }
}
