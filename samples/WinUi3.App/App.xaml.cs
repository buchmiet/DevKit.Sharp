using DevKit.Screenshot.Sharp;
using DevKit.Screenshot.WinUi3.Sharp;
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
        var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var screenshotOptions = ScreenshotArgs.ParseAndRemove(ref cliArgs);

        var services = new ServiceCollection();
        services.AddSingleton(screenshotOptions);
        services.AddScreenshot(_ => _window
            ?? throw new InvalidOperationException("Main window has not been created yet."));
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
        _window.AttachScreenshot(screenshotOptions);
        _window.Activate();
    }
}
