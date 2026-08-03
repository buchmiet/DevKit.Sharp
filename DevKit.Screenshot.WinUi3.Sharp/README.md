# DevKit.Screenshot.WinUi3.Sharp

Main-window screenshot capture for **WinUI 3** (Windows App SDK) apps — a one-liner CI hook plus
a programmatic `IScreenshot` service. Built for CI pipelines, visual testing and dev tooling.

## Quick start

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
    var options = ScreenshotArgs.ParseAndRemove(ref cliArgs);   // strips --devkit-screenshot…

    _window = new MainWindow();
    _window.AttachScreenshot(options);   // captures on first activation, after render
    _window.Activate();
}
```

Run your app:

```text
MyApp.exe --devkit-screenshot artifacts/shot.png --devkit-screenshot-exit --devkit-screenshot-delay 500
MyApp.exe --devkit-screenshot-clipboard --devkit-screenshot-exit
```

The window activates, renders, waits the settle delay, saves the PNG and (with `-exit`) exits.
On failure the error goes to stderr and the process exits with code 70.

Without the CLI switch the hook is a no-op — ship it in production code freely.

## Programmatic use

```csharp
// Preferred: tell the library where your window lives.
services.AddScreenshot(_ => _window!);

// Fallback: reflection-based resolution — requires a public MainWindow property
// or the template's m_window field on your Application class.
services.AddScreenshot();

var result = await screenshot.CaptureMainWindowAsync("shot.png");

await screenshot.CaptureMainWindowToClipboardAsync();
```

### Capture an element (panel, terminal surface, …)

```csharp
var (width, height, pngBytes) = await ElementScreenshotCapture.CapturePngAsync(myBorder);
await ElementScreenshotCapture.CopyToClipboardAsync(myBorder);
```

Element capture flattens transparency by default. Opt out with
`new ElementCaptureOptions { FlattenTransparency = false }` or supply an explicit `Background` color.

Part of the [DevKit.Sharp](https://github.com/buchmiet/DevKit.Sharp) family. MIT licensed.
