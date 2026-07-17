# DevTools.Screenshot.Avalonia.Sharp

Main-window screenshot capture for **Avalonia** apps — a one-liner CI hook plus a programmatic
`IScreenshot` service. Built for CI pipelines, visual testing and dev tooling.

## Quick start

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var args = desktop.Args ?? [];
        var options = ScreenshotArgs.ParseAndRemove(ref args);   // strips --devtools-screenshot…

        desktop.MainWindow = new MainWindow();
        desktop.AttachScreenshot(options);   // captures once the window opens and renders
    }

    base.OnFrameworkInitializationCompleted();
}
```

Run your app:

```text
MyApp.exe --devtools-screenshot artifacts/shot.png --devtools-screenshot-exit --devtools-screenshot-delay 300
```

The window opens, renders, waits the settle delay, saves the PNG and (with `-exit`) shuts down.
On failure the error goes to stderr and the app exits with code 70 — return the lifetime's exit
code from `Main` to propagate it:

```csharp
[STAThread]
public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

Without the CLI switch the hook is a no-op — ship it in production code freely.

## Programmatic use

```csharp
services.AddScreenshot();                    // registers IScreenshot

var result = await screenshot.CaptureMainWindowAsync("shot.png");
// result.OutputPath, result.PixelWidth, result.PixelHeight
```

Part of the [DevTools.Sharp](https://github.com/buchmiet/DevTools.Sharp) family. MIT licensed.
