# DevTools.HostLogging.Sharp

A tiny **startup reporter** for .NET hosts — timestamped boot lines and an optional progress bar.

Not a general logger (use Serilog/NLog at runtime). Not a splash screen.

## Quick start

```csharp
public static void Main(string[] args)
{
    using var boot = HostLog.Open(ref args);   // strips --devtools-hostlog … from args

    boot.BeginProgress(4);
    boot.Write("Starting…");
    boot.CompleteStep("Host built");

    RunApp(args);   // remaining CLI args
}
```

### Command line

```text
MyApp.exe --devtools-hostlog console
MyApp.exe --devtools-hostlog file C:\temp\boot.log
MyApp.exe                                  → logger disabled (zero overhead)
```

| Flag | Behaviour |
|------|-----------|
| `--devtools-hostlog console` | Text + progress bar in a console (allocates one on Windows GUI) |
| `--devtools-hostlog file <path>` | Boot log file, overwritten on each run |
| *(none)* | `NullStartupSession` — zero overhead |

Malformed switches are reported on stderr and ignored.

## Host types

| Host | `--devtools-hostlog console` |
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

`DevTools.HostLogging.Runner` (in the repo, not on NuGet) hosts the startup console in a separate
process and receives events over a named pipe — useful when the app itself must stay console-free.
It sets the `DEVTOOLS_HOSTLOG_ATTACH` environment variable for the spawned client;
`HostLog.Attach()` picks it up.

Part of the [DevTools.Sharp](https://github.com/buchmiet/DevTools.Sharp) family. MIT licensed.
