# DevTools.Sharp

Small, focused dev-tooling packages for .NET desktop apps. Each ships as a separate NuGet package.

| Package | What it does |
|---------|--------------|
| [`DevTools.Screenshot.Sharp`](DevTools.Screenshot.Sharp/README.md) | Contract + `--devtools-screenshot` CLI parser for window captures |
| [`DevTools.Screenshot.Avalonia.Sharp`](DevTools.Screenshot.Avalonia.Sharp/README.md) | Main-window capture for Avalonia 12 |
| [`DevTools.Screenshot.WinUi3.Sharp`](DevTools.Screenshot.WinUi3.Sharp/README.md) | Main-window capture for WinUI 3 |
| [`DevTools.HostLogging.Sharp`](DevTools.HostLogging.Sharp/README.md) | Startup-phase reporter: boot lines + console progress bar |

## Design

One convention across the family: every tool is driven by a namespaced CLI switch
(`--devtools-screenshot…`, `--devtools-hostlog…`), parsed with a `ParseAndRemove(ref args)`
helper that strips its tokens before your app sees them. With no switch present every hook is a
no-op, so the wiring can ship in production code.

```csharp
// Avalonia app, in OnFrameworkInitializationCompleted:
var options = ScreenshotArgs.ParseAndRemove(ref args);
desktop.MainWindow = mainWindow;
desktop.AttachScreenshot(options);

// WinUI 3 app, in OnLaunched:
var options = ScreenshotArgs.ParseAndRemove(ref cliArgs);
window.AttachScreenshot(options);
window.Activate();

// Any host, first lines of Main:
using var boot = HostLog.Open(ref args);
```

Same CLI, same behaviour, no view-model changes:

```text
MyApp.exe --devtools-screenshot artifacts/shot.png --devtools-screenshot-exit
MyApp.exe --devtools-hostlog console
```

## Build

```powershell
dotnet build DevTools.Sharp.slnx
dotnet run --project tests/DevTools.HostLogging.Sharp.Tests -c Release
dotnet run --project tests/DevTools.Screenshot.Sharp.Tests -c Release
```

## Samples

`samples/` contains twin Avalonia and WinUI 3 apps sharing one view-model project, plus
console/GUI hosts for HostLogging:

```powershell
dotnet run --project samples/Avalonia.App -- --devtools-screenshot artifacts/screenshots/avalonia.png --devtools-screenshot-exit
dotnet run --project samples/WinUi3.App -- --devtools-screenshot artifacts/screenshots/winui3.png --devtools-screenshot-exit
dotnet run --project samples/HostLogging.Sample.Console -- --devtools-hostlog console
```

## Packing

```powershell
dotnet pack DevTools.Sharp.slnx -c Release
```

## Requirements

- .NET SDK 10 to build the repo; packages target `netstandard2.0`/`net8.0`/`net10.0`
- Avalonia 12+ (Avalonia package), Windows App SDK 2.2 + Windows 10 17763+ (WinUI 3 package)

MIT licensed.
