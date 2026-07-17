using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevTools.ScreenShot.Avalonia.Sharp;
using Microsoft.Extensions.DependencyInjection;
using Sample.ViewModels;
using ViewsMainWindow = Avalonia.Views.MainWindow;

namespace Avalonia.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = ScreenshotOptionsParser.Parse(desktop.Args ?? []);
            var services = new ServiceCollection();
            services.AddSingleton(options);
            services.AddScreenShot();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ViewsMainWindow>();
            _services = services.BuildServiceProvider();

            var mainWindow = _services.GetRequiredService<ViewsMainWindow>();
            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += (_, _) =>
            {
                _services?.Dispose();
                _services = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
