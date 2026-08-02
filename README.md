# DevKit.Sharp

Small, focused dev-tooling packages for .NET desktop apps. Each ships as a separate NuGet package.

## NuGet packages

| Package | What it does | Install |
|---------|--------------|---------|
| [DevKit.Screenshot.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.Sharp) | Contract + `--devkit-screenshot` CLI parser | `dotnet add package DevKit.Screenshot.Sharp` |
| [DevKit.Screenshot.Avalonia.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.Avalonia.Sharp) | Main-window capture for Avalonia 12 | `dotnet add package DevKit.Screenshot.Avalonia.Sharp` |
| [DevKit.Screenshot.WinUi3.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.WinUi3.Sharp) | Main-window capture for WinUI 3 | `dotnet add package DevKit.Screenshot.WinUi3.Sharp` |
| [DevKit.Logging.Sharp](https://www.nuget.org/packages/DevKit.Logging.Sharp) | Startup-phase reporter: boot lines + console progress bar | `dotnet add package DevKit.Logging.Sharp` |

> **Versioning:** `0.1.0` is the first public release under the DevKit name.

## Design

One convention across the family: every tool is driven by a namespaced CLI switch
(`--devkit-screenshot…`, `--devkit-logging…`), parsed with a `ParseAndRemove(ref args)`
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
MyApp.exe --devkit-screenshot artifacts/shot.png --devkit-screenshot-exit
MyApp.exe --devkit-screenshot-clipboard --devkit-screenshot-exit
MyApp.exe --devkit-logging console
```

## Development

Requirements: .NET 10 SDK, Python 3 (for the boundary check).

```powershell
python eng/verify-contract-boundary.py
dotnet build DevKit.Sharp.slnx -c Release
dotnet run --project tests/DevKit.Logging.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Screenshot.Sharp.Tests -c Release
```

### Local package build

```powershell
dotnet pack DevKit.Sharp.slnx -c Release -o artifacts/packages
```

The default local version is `0.1.0` (see `Directory.Build.props`). Override for a dry run:

```powershell
dotnet pack DevKit.Sharp.slnx -c Release -o artifacts/packages -p:Version=0.1.1
```

## Samples

`samples/` contains twin Avalonia and WinUI 3 apps sharing one view-model project, plus
console/GUI hosts for Logging:

```powershell
dotnet run --project samples/Avalonia.App -- --devkit-screenshot artifacts/screenshots/avalonia.png --devkit-screenshot-exit
dotnet run --project samples/WinUi3.App -- --devkit-screenshot artifacts/screenshots/winui3.png --devkit-screenshot-exit
dotnet run --project samples/Logging.Sample.Console -- --devkit-logging console
```

## Publishing

CI runs on every push and pull request to `main`. Packages are published to NuGet.org when a
version tag is pushed:

```text
git tag v0.1.0
git push origin v0.1.0
```

Set the `NUGET_API_KEY` secret in the GitHub `nuget` environment before the first publish.

## Requirements

- .NET SDK 10 to build the repo; packages target `netstandard2.0`/`net8.0`/`net10.0`
- Avalonia 12+ (Avalonia package), Windows App SDK 2.2 + Windows 10 17763+ (WinUI 3 package)

## License

MIT — see [LICENSE](LICENSE).
