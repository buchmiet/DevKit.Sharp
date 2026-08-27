# DevKit.Sharp

Small, focused dev-tooling packages for .NET desktop apps. Each ships as a separate NuGet package
from this monorepo.

## Layout

```text
DevKit.Screenshot.Sharp/              contract (netstandard2.0; net8.0)
DevKit.Screenshot.Avalonia.Sharp/     Avalonia adapter
DevKit.Screenshot.WinUi3.Sharp/       WinUI 3 adapter
DevKit.Focus.Sharp/                   contract (netstandard2.0; net8.0)
DevKit.Focus.Avalonia.Sharp/          Avalonia adapter
DevKit.Focus.WinUi3.Sharp/            WinUI 3 adapter
DevKit.Logging.Sharp/                 startup reporter
DevKit.Logging.Runner/                companion exe (not a nupkg)
tests/DevKit.*.Tests/                 one TUnit project per package
samples/                              twin Avalonia / WinUI apps + logging hosts
eng/verify-contract-boundary.py       Screenshot.Sharp + Focus.Sharp stay UI-free
```

Adapters reference their contract with `ProjectReference`. There is no sibling-repo checkout.

## NuGet packages

| Package | What it does | Install |
|---------|--------------|---------|
| [DevKit.Screenshot.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.Sharp) | Contract + `--devkit-screenshot` CLI parser | `dotnet add package DevKit.Screenshot.Sharp` |
| [DevKit.Screenshot.Avalonia.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.Avalonia.Sharp) | Main-window + element capture for Avalonia 12 | `dotnet add package DevKit.Screenshot.Avalonia.Sharp` |
| [DevKit.Screenshot.WinUi3.Sharp](https://www.nuget.org/packages/DevKit.Screenshot.WinUi3.Sharp) | Main-window + element capture for WinUI 3 | `dotnet add package DevKit.Screenshot.WinUi3.Sharp` |
| [DevKit.Focus.Sharp](https://www.nuget.org/packages/DevKit.Focus.Sharp) | Framework-neutral keyboard-focus policy | `dotnet add package DevKit.Focus.Sharp` |
| [DevKit.Focus.Avalonia.Sharp](https://www.nuget.org/packages/DevKit.Focus.Avalonia.Sharp) | Avalonia 12 focus scopes | `dotnet add package DevKit.Focus.Avalonia.Sharp` |
| [DevKit.Focus.WinUi3.Sharp](https://www.nuget.org/packages/DevKit.Focus.WinUi3.Sharp) | WinUI 3 focus scopes | `dotnet add package DevKit.Focus.WinUi3.Sharp` |
| [DevKit.Logging.Sharp](https://www.nuget.org/packages/DevKit.Logging.Sharp) | Startup-phase reporter: boot lines + console progress bar | `dotnet add package DevKit.Logging.Sharp` |

> **Versioning:** each packable project has its own `<Version>` (currently `0.3.0`). Do not lockstep the family — see [Versioning](#versioning). `0.1.0` was the first public release under the DevKit name.

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
dotnet run --project tests/DevKit.Screenshot.Avalonia.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Screenshot.WinUi3.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Focus.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Focus.Avalonia.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Focus.WinUi3.Sharp.Tests -c Release
```

### Local package build

```powershell
dotnet pack DevKit.Sharp.slnx -c Release -o artifacts/packages
```

The version of each nupkg comes from that project's `<Version>` (not a shared `Directory.Build.props` pin). Override one pack for a dry run:

```powershell
dotnet pack DevKit.Screenshot.WinUi3.Sharp/DevKit.Screenshot.WinUi3.Sharp.csproj -c Release -o artifacts/packages -p:Version=0.3.1-local
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
`v*` tag is pushed. The tag is a **release marker**; each nupkg uses the `<Version>` in its
`.csproj`. Already-published versions are skipped (`--skip-duplicate`), so you can tag after
bumping only the WinUI adapter.

```text
git tag v0.3.0
git push origin v0.3.0
```

To publish a single package (workflow_dispatch `project` input), pass a repo-relative csproj
path. Optional `version` input overrides every packed project — leave it empty for normal
independent versions.

Set the `NUGET_API_KEY` secret in the GitHub `nuget` environment before the first publish.

## Versioning

Stay on **0.x SemVer** until a 1.0 contract freeze. In 0.x:

| Bump | When | Which package |
|------|------|----------------|
| **MINOR** (`0.3.0` → `0.4.0`) | Raise the **minimum** UI/runtime dependency (Windows App SDK 2.2 → 2.4, Avalonia 12.1 → 12.2), or add public API | Only the adapter that pins that dependency |
| **PATCH** (`0.3.0` → `0.3.1`) | Bugfix, tests, docs; min-dep unchanged | Only the package that changed |
| **New package** `0.1.0` | New UI stack (MAUI, Uno, …) | New `DevKit.<Tool>.<Ui>.Sharp` — do not inflate Avalonia/WinUI versions |

Rules that make UI-library bumps possible without dragging Logging:

1. **One `<Version>` per packable csproj.** Never a shared family pin (that is what blocked a WinUI-only WASDK bump).
2. **Min version in `PackageReference` is the consumer floor.** Raising `Microsoft.WindowsAppSDK` from `2.2.0` to `2.4.0` is a MINOR of `DevKit.Screenshot.WinUi3.Sharp` only. Consumers on 2.2 cannot restore that nupkg without taking 2.4 — that is intentional if your app already runs 2.4.
3. **New UI library = new package**, not a version of an existing adapter.
4. Tag `v*` publishes whatever versions are in the tree. Leave unchanged packages at their last number; NuGet skip-duplicate ignores them.

This 0.3.0 line: WinUI min SDK is **2.4.0**. Avalonia adapters require Avalonia **12.1.1**.
`Microsoft.Extensions.DependencyInjection.Abstractions` on the screenshot UI adapters is **8.0.2** (`net8`) / **10.0.11** (`net10`).

## Requirements

- .NET SDK 10 to build the repo; packages target `netstandard2.0`/`net8.0`/`net10.0`
- Avalonia **12.1.1** (Avalonia packages), Windows App SDK **2.4** + Windows 10 17763+ (WinUI 3 packages)
- `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.2 (`net8`) / 10.0.11 (`net10`) on the UI screenshot packages

## License

MIT — see [LICENSE](LICENSE).
