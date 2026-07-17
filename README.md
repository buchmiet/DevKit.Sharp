# DevTools.ScreenShot

Biblioteki do robienia screenshotów głównego okna aplikacji desktopowej — pod CI, testy wizualne i dev tooling.

**Wersja:** 0.0.1

## Projekty

| Projekt | Opis |
|---------|------|
| `DevTools.ScreenShot.Sharp` | Kontrakt: `IScreenShot` |
| `DevTools.ScreenShot.Avalonia.Sharp` | Implementacja dla Avalonia 12 |
| `DevTools.ScreenShot.WinUi3.Sharp` | Implementacja dla WinUI 3 |

## API

```csharp
public interface IScreenShot
{
    Task CaptureMainWindowAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);
    Task CaptureMainWindowAndExitAsync(string outputPath, int delayMs = 0, CancellationToken cancellationToken = default);
}
```

Rejestracja DI:

```csharp
// Avalonia
services.AddScreenShot();

// WinUI 3
services.AddScreenShot();
```

### WinUI 3 — wymaganie hosta

`DevTools.ScreenShot.WinUi3.Sharp` rozwiązuje główne okno przez refleksję. Na klasie `Application` musi być publiczna właściwość `MainWindow` (jak w sample `WinUi3.App`).

## Samples

W folderze `samples/` są dwie aplikacje testowe (MVVM, wzorzec jak NanoCommander):

```
samples/
├── Sample.ViewModels/      # wspólne VM + parser CLI
├── Avalonia.Views/         # widoki Avalonia
├── Avalonia.App/           # host Avalonia
├── WinUi3.Views/           # helpery UI WinUI
└── WinUi3.App/             # host WinUI (MainWindow.xaml tutaj)
```

Screenshot przy starcie — wywołanie z `MainWindowViewModel.OnLoadedAsync()` gdy podasz `--screenshot`.

### Uruchomienie

```powershell
dotnet run --project samples/Avalonia.App -- --screenshot artifacts/screenshots/avalonia.png --exit --delay 300
dotnet run --project samples/WinUi3.App -- --screenshot artifacts/screenshots/winui3.png --exit --delay 500
```

### Flagi CLI (samples)

| Flaga | Opis |
|-------|------|
| `--screenshot <path>` | Ścieżka pliku PNG |
| `--exit` | Zamknij aplikację po zapisie |
| `--delay <ms>` | Opóźnienie przed capture (domyślnie 0) |

## Build

```powershell
dotnet build DevTools.ScreenShot.slnx
```

## Wymagania

- .NET 10
- Avalonia 12 (projekt Avalonia)
- Windows App SDK 2.2 + Windows 10 19041+ (projekt WinUI 3)
