using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sample.ViewModels;
using WinRT.Interop;

namespace WinUi3.Views;

public sealed partial class MainWindow : Window
{
    private bool _screenshotScheduled;

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
        Activated += OnActivated;
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_screenshotScheduled || args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        _screenshotScheduled = true;
        Activated -= OnActivated;
        await ViewModel.OnLoadedAsync();
    }
}
