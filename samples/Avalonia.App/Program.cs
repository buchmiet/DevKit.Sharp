using Avalonia;

namespace Avalonia.App;

internal static class Program
{
    // int Main: propagates the exit code from desktop.Shutdown(code), which the
    // screenshot hook uses to signal capture failures to CI.
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
