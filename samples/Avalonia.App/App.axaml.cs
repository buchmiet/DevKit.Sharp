using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevTools.Screenshot.Avalonia.Sharp;
using DevTools.Screenshot.Sharp;
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
            var args = desktop.Args ?? [];
            var screenshotOptions = ScreenshotArgs.ParseAndRemove(ref args);

            var services = new ServiceCollection();
            services.AddSingleton(screenshotOptions);
            services.AddScreenshot();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ViewsMainWindow>();
            _services = services.BuildServiceProvider();

            desktop.MainWindow = _services.GetRequiredService<ViewsMainWindow>();
            desktop.AttachScreenshot(screenshotOptions);
            desktop.ShutdownRequested += (_, _) =>
            {
                _services?.Dispose();
                _services = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
