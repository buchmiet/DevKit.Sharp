# DevKit.Screenshot.Sharp

Framework-agnostic contract for capturing app-window screenshots — built for CI pipelines,
visual testing and dev tooling. Pair it with an implementation package:

| Package | Framework |
|---------|-----------|
| `DevKit.Screenshot.Avalonia.Sharp` | Avalonia 12 |
| `DevKit.Screenshot.WinUi3.Sharp` | WinUI 3 (Windows App SDK) |

## What's inside

```csharp
public interface IScreenshot
{
    Task<ScreenshotResult> CaptureMainWindowAsync(
        ScreenshotOptions options, CancellationToken cancellationToken = default);
}

public sealed class ScreenshotOptions
{
    public string?  OutputPath { get; init; }          // PNG path
    public bool     CopyToClipboard { get; init; }    // system clipboard instead of file
    public TimeSpan Delay { get; init; }               // settle delay, default 150 ms
    public bool     ExitAfterCapture { get; init; }    // used by AttachScreenshot hooks
    public bool     IsEnabled { get; }
}

public readonly record struct ScreenshotResult(
    string? OutputPath, int PixelWidth, int PixelHeight, bool CopiedToClipboard = false);
```

## CLI parsing

`ScreenshotArgs.ParseAndRemove(ref args)` understands the switch family below and removes the
tokens from `args`, so your app never sees them:

| Switch | Meaning |
|--------|---------|
| `--devkit-screenshot <path>` | enable capture, write PNG to `<path>` |
| `--devkit-screenshot-clipboard` | enable capture, copy PNG to the system clipboard |
| `--devkit-screenshot-exit` | close the app after the capture |
| `--devkit-screenshot-delay <ms>` | settle delay before capture (default 150) |

```csharp
var options = ScreenshotArgs.ParseAndRemove(ref args);
```

Capture failures in the `AttachScreenshot` hooks exit with code
`ScreenshotExitCodes.CaptureFailed` (70), so CI can tell "no screenshot" from success.

Part of the [DevKit.Sharp](https://github.com/buchmiet/DevKit.Sharp) family. MIT licensed.
