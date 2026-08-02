# Implementation brief — issue #1: public element/screenshot capture API

Target: https://github.com/buchmiet/DevKit.Sharp/issues/1
Release: **0.1.1**. The PR/commit must close the issue (`Closes #1`).

## Current state (verified)

- **Avalonia side is DONE but uncommitted** in the working tree:
  - `DevKit.Screenshot.Avalonia.Sharp/VisualScreenshotCapture.cs` — public static helper, full XML docs.
  - `samples/Avalonia.Views` uses the package API; the sample-local copy was deleted; the sample project references the package project.
  - `DevKit.Screenshot.Avalonia.Sharp/README.md` documents subtree capture.
  - **Do not rewrite this.** Review it lightly for style consistency and keep it as-is unless something is objectively wrong.
- **WinUI side is MISSING** — this is your main task:
  - Element capture exists only in `samples/WinUi3.Views/ElementScreenshotCapture.cs` (namespace `WinUi3.Views`).
  - `DevKit.Screenshot.WinUi3.Sharp` has no element capture API.
  - `DevKit.Screenshot.WinUi3.Sharp/README.md` does not document element capture.

## Scope (from the issue — do not exceed it)

**Non-goals:** do NOT change `IScreenshot` in `DevKit.Screenshot.Sharp`; do NOT add CLI switches for arbitrary elements. The contract assembly must stay untouched (`python eng/verify-contract-boundary.py` must pass).

## Tasks

### 1. Add `ElementScreenshotCapture` to `DevKit.Screenshot.WinUi3.Sharp`

Create `DevKit.Screenshot.WinUi3.Sharp/ElementScreenshotCapture.cs`:

- Namespace `DevKit.Screenshot.WinUi3.Sharp`, `public static class ElementScreenshotCapture`.
- Base it on `samples/WinUi3.Views/ElementScreenshotCapture.cs`, adapting to package conventions:
  - `public static Task<(int Width, int Height, byte[] PngBytes)> CapturePngAsync(FrameworkElement element)`
  - `public static Task<(int Width, int Height)> CopyToClipboardAsync(FrameworkElement element)`
  - `public static Task<(string Path, BitmapImage Preview)?> SaveToFileAsync(Window window, FrameworkElement element, string suggestedFileName)`
    (the `Window` parameter is required for `InitializeWithWindow` on the file picker — keep it)
  - Add an optional `CancellationToken cancellationToken = default` parameter to all three methods, checked/cooperative like `WinUiScreenshot` does (the package style), threading it into the encode/save steps. This matches the existing package API style; the sample-local version lacks it.
- Full XML `<summary>` doc comments on every public member (project has `GenerateDocumentationFile=true`; follow the doc style of `WinUiScreenshot.cs` / `VisualScreenshotCapture.cs`). Mirror the Avalonia doc phrasing, e.g. "Captures an arbitrary `FrameworkElement`… Use `WinUiScreenshot` or `IScreenshot` for the main window."
- `ArgumentNullException.ThrowIfNull` for reference parameters, matching package style.
- Keep the class **static** — deliberate design: element capture is bound to a live element on the UI thread, not to a window lifetime, so DI registration (unlike `IScreenshot`) adds nothing. Symmetric with Avalonia's `VisualScreenshotCapture`.

### 2. Deduplicate the PNG encoder (small, mechanical refactor)

`WinUiScreenshot.cs` already contains a private `EncodePngAsync` identical in shape to the one you need. Extract an `internal static` helper (e.g. `internal static class PngEncoder` in its own file `PngEncoder.cs`) with a single `EncodeAsync(IRandomAccessStream, IBuffer pixels, int width, int height, double dpi, CancellationToken)` method, and call it from **both** `WinUiScreenshot` and `ElementScreenshotCapture`. No behavior change in `WinUiScreenshot` — same call sequence, same parameters.

### 3. Update the WinUI sample to use the package API

- Delete `samples/WinUi3.Views/ElementScreenshotCapture.cs`.
- In `samples/WinUi3.App/Views/MainWindow.xaml.cs` add `using DevKit.Screenshot.WinUi3.Sharp;`. Note: `MainWindow` is declared in namespace `WinUi3.Views` while living in the `WinUi3.App` project — after deleting the sample helper the name `ElementScreenshotCapture` will resolve to the package class via the new using. `WinUi3.App.csproj` already references `DevKit.Screenshot.WinUi3.Sharp` — no csproj change needed.
- The sample call sites pass no `CancellationToken` — the new optional parameter keeps them source-compatible. Do not change call sites.

### 4. Documentation

- `DevKit.Screenshot.WinUi3.Sharp/README.md`: add a "Capture an element (panel, terminal surface, …)" subsection under "Programmatic use", mirroring the Avalonia README's subtree section:

  ```csharp
  var (width, height, pngBytes) = await ElementScreenshotCapture.CapturePngAsync(myBorder);
  await ElementScreenshotCapture.CopyToClipboardAsync(myBorder);
  ```

- Update the `<Description>` in `DevKit.Screenshot.WinUi3.Sharp.csproj` and `DevKit.Screenshot.Avalonia.Sharp.csproj` to also mention element/subtree capture (one short clause, e.g. "…plus VisualScreenshotCapture/ElementScreenshotCapture for arbitrary elements"). Do not touch tags or versions.
- Root `README.md`: in the packages table, extend the two screenshot rows minimally (e.g. "Main-window + element capture for Avalonia 12"). No other root README changes.

### 5. Version bump

`Directory.Build.props`: `<Version>0.1.0</Version>` → `0.1.1` (local default; the publish workflow overrides from the tag anyway). Leave `AssemblyVersion`/`FileVersion` at `0.0.0.0`.

## Verification (all must pass before handing back)

```powershell
python eng/verify-contract-boundary.py
dotnet build DevKit.Sharp.slnx -c Release
dotnet run --project tests/DevKit.Logging.Sharp.Tests -c Release
dotnet run --project tests/DevKit.Screenshot.Sharp.Tests -c Release
dotnet pack DevKit.Sharp.slnx -c Release -o artifacts/packages
```

- Build must produce **0 errors** and no *new* warnings (the pre-existing `AVLN3001` on the Avalonia sample is known and out of scope).
- Packing is part of verification — CI packs the whole solution; a malformed pack (missing README, doc file) must fail here, not in CI.
- Unit tests for the capture helpers are **not required**: rendering needs a live UI thread/compositor, and the acceptance criteria explicitly allow "integration coverage via sample build" — the solution build compiles both samples against the new API, which is the agreed coverage.

## Definition of done (maps to issue acceptance criteria)

- [ ] `DevKit.Screenshot.Avalonia.Sharp` exposes public subtree capture (already present — verified, unchanged)
- [ ] `DevKit.Screenshot.WinUi3.Sharp` exposes equivalent element capture
- [ ] Both package READMEs document element capture
- [ ] No capture helper remains under `samples/*`; samples compile against the package API
- [ ] Boundary check, Release build, tests, pack — all green
- [ ] Commit message references `Closes #1`

## Style notes for this repo

- C# latest, nullable on, implicit usings; file-scoped namespaces; expression-bodied members where natural.
- `ArgumentNullException.ThrowIfNull`, `InvalidOperationException` with short factual messages ("Element has no XamlRoot.", "Element has no measurable area to capture.").
- Keep it minimal: no new abstractions beyond the encoder extraction, no options classes, no new dependencies in any csproj.
