# DevKit.Logging.Sharp

A tiny **startup reporter** for .NET hosts — timestamped boot lines and an optional progress bar.

Not a general logger (use Serilog/NLog at runtime). Not a splash screen.

## Quick start

```csharp
public static void Main(string[] args)
{
    using var boot = HostLog.Open(ref args);   // strips --devkit-logging … from args

    boot.BeginProgress(4);
    boot.Write("Starting…");
    boot.CompleteStep("Host built");

    RunApp(args);   // remaining CLI args
}
```

### Command line

```text
MyApp.exe --devkit-logging console
MyApp.exe --devkit-logging file C:\temp\boot.log
MyApp.exe                                  → logger disabled (zero overhead)
```

| Flag | Behaviour |
|------|-----------|
| `--devkit-logging console` | Text + progress bar in a console (allocates one on Windows GUI) |
| `--devkit-logging file <path>` | Boot log file, overwritten on each run |
| *(none)* | `NullStartupSession` — zero overhead |

Malformed switches are reported on stderr and ignored.

## Host types

| Host | `--devkit-logging console` |
|------|------------------------------|
| Console `Exe` | log in the same terminal |
| GUI `WinExe` | separate console window (`AllocConsole`), auto-closed when progress completes |
| Any + `file` | no console needed |

**Same `Main` code for all hosts** — only the CLI differs. In a GUI app you watch boot progress
on a live console instead of staring at nothing for a few seconds.

## API

```csharp
HostLog.Open(ref string[] args)      // parse CLI + open session
HostLog.Open(HostLogOptions)         // programmatic
HostLog.Attach(string? name = null)  // remote runner / named pipe

IStartupSession : IDisposable
  bool IsEnabled
  void Write(string message)
  void BeginProgress(int totalSteps)   // nested calls open sub-phases
  void CompleteStep(string? message = null)
  void Close()
```

## Detached runner

`DevKit.Logging.Runner` (in the repo, not on NuGet) hosts the startup console in a separate
process and receives events over a named pipe — useful when the app itself must stay console-free.
It sets the `DEVKIT_LOGGING_ATTACH` environment variable for the spawned client;
`HostLog.Attach()` picks it up.

Part of the [DevKit.Sharp](https://github.com/buchmiet/DevKit.Sharp) family. MIT licensed.
