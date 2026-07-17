using DevTools.ScreenShot.WinUi3.Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Sample.ViewModels;
using ViewsMainWindow = WinUi3.Views.MainWindow;

namespace WinUi3.App;

public partial class App : Application
{
    private ServiceProvider? _services;
    private ViewsMainWindow? _window;

    public Window? MainWindow => _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var options = ScreenshotOptionsParser.Parse(
            Environment.GetCommandLineArgs().Skip(1).ToArray());

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddScreenShot();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ViewsMainWindow>();
        _services = services.BuildServiceProvider();

        _window = _services.GetRequiredService<ViewsMainWindow>();
        _window.Closed += (_, _) =>
        {
            _services?.Dispose();
            _services = null;
            _window = null;
        };
        _window.Activate();
    }
}
