using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace DevKit.Focus.Avalonia.Sharp.Tests;

public sealed class HeadlessTestApp : Application
{
    public HeadlessTestApp()
    {
        Styles.Add(new SimpleTheme());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true,
            });
}
