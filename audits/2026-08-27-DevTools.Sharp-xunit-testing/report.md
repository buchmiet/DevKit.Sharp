# Przegląd projektów testowych — DevTools.Sharp

**Pakiet audytu:** `xunit-testing` (Khorikov, autorytet 4 — strategia/wartość; dokumentacja xUnit.net, autorytet 5 — mechanika)
**Cel:** `C:\Users\newon\source\repos\DevTools.Sharp` (commit `3522e36`, gałąź `main`)
**Data:** 2026-08-27
**Prior runs:** brak folderów `*-xunit-testing` pod `audits/` — pierwszy przebieg. Brak sibling-raportów w tym repozytorium.

> **Zakres frameworka:** suite jest na **TUnit 1.13.11** (Microsoft Testing Platform v2.1.0, `Engine Mode: SourceGenerated`), nie na xUnit.net. Karty mechaniki xUnit (`xunit.analyzers`, `[Fact]`/`[Theory]`, `Collection`, `xunit.runner.json`) **nie stosują się**. Strategia, topologia i CI — tak. Nie rekomenduję migracji na xUnit. Luka kompendium: GAPS w `audits/xunit-testing`.

## As-Is Test Architecture

**Rozwiązanie / projekty**

- Jedno rozwiązanie: `DevKit.Sharp.slnx` (16 projektów na dysku = 16 w slnx).
- Trzy projekty testowe, wszystkie `net10.0`, `IsPackable=false`, `TestingPlatformDotnetTestSupport=true`:

| Projekt | `ProjectReference` | Pakiety testowe |
|---------|--------------------|-----------------|
| `tests/DevKit.Logging.Sharp.Tests` | `DevKit.Logging.Sharp` | TUnit 1.13.11, Microsoft.NET.Test.Sdk 17.14.1 |
| `tests/DevKit.Screenshot.Sharp.Tests` | `DevKit.Screenshot.Sharp` | to samo |
| `tests/DevKit.Screenshot.Avalonia.Sharp.Tests` | `DevKit.Screenshot.Avalonia.Sharp` (+ Avalonia 12.1.0 / Headless / Skia / Simple) | to samo |

- Brak `tests/DevKit.Screenshot.WinUi3.Sharp.Tests` (paczka `DevKit.Screenshot.WinUi3.Sharp` jest `IsPackable=true` i publikowana na NuGet jako 0.1.2).
- Brak `xunit`, `xunit.v3`, `xunit.runner.visualstudio`, `xunit.analyzers`. Atrybut testu: TUnit `[Test]` (22 metody). Zero `[Fact]` / `[Theory]`.
- `global.json` pinuje SDK 10.0.100; **nie** ustawia `"test": { "runner": "Microsoft.Testing.Platform" }`. Dual posture MTP (`TestingPlatformDotnetTestSupport`) + `Microsoft.NET.Test.Sdk` — routing do `modern-dotnet-architecture-review`.
- `DevKit.Logging.Sharp` ma `InternalsVisibleTo` na `DevKit.Logging.Runner` i `DevKit.Logging.Sharp.Tests`. Testy parsera i trackera idą w typy `internal`.

**Pliki i liczby**

| Plik | Klasy | `[Test]` |
|------|-------|----------|
| `tests/DevKit.Logging.Sharp.Tests/HostLogArgsParserTests.cs` | 1 | 4 |
| `tests/DevKit.Logging.Sharp.Tests/NestedProgressTrackerTests.cs` | 1 | 3 |
| `tests/DevKit.Screenshot.Sharp.Tests/ScreenshotArgsTests.cs` | 1 | 12 |
| `tests/DevKit.Screenshot.Avalonia.Sharp.Tests/VisualScreenshotCaptureTests.cs` | 1 | 3 |
| `HeadlessTestApp.cs` / `HeadlessTestAssembly.cs` / `BitmapTestHelpers.cs` | harness | 0 |

Razem **22** testy. Zero `[Arguments]` / data-driven.

**Konfiguracja**

- Brak `xunit.runner.json`, `testconfig.json`, `.runsettings`.
- Brak `[Trait]` / `Category`, `[Collection]`, `IClassFixture`, `DisableParallelization`.
- Avalonia: `[assembly: AvaloniaTestApplication]` + `AvaloniaTestIsolationLevel.PerTest`; statyczna `HeadlessUnitTestSession` w klasie testowej (wzorzec headless Avalonia, nie shared mutable fixture domenowy).
- Zero mocków (`Moq` / `NSubstitute` / `FakeItEasy`), zero `Thread.Sleep` / `Task.Delay`, zero refleksji na składowe prywatne.

**CI**

- `.github/workflows/ci.yml`: push/PR na `main`, `windows-latest`, jedno job.
- Kroki: `python eng/verify-contract-boundary.py` → restore → build Release → **trzy** `dotnet run --project tests/… -c Release --no-build` → pack. Brak `dotnet test`.
- Brak `--filter` / query filter. Brak publikacji TRX/JUnit. Artifact to tylko `*.nupkg`.
- `publish.yml` packuje tag `v*` — testów nie odpala.

**Pomiar (ta sesja, Windows, Release, HEAD `3522e36`)**

| Projekt | total | failed | duration (silnik) | wall `dotnet run` |
|---------|-------|--------|-------------------|-------------------|
| Logging.Sharp.Tests | 7 | 0 | 189 ms | 1520 ms |
| Screenshot.Sharp.Tests | 12 | 0 | 181 ms | 1520 ms |
| Screenshot.Avalonia.Sharp.Tests | 3 | 0 | 579 ms | 1794 ms |

22/22 zielone. Cała suite ≪ 5 min — split fast/slow nie jest wymuszony czasem.

**Styl (próbka)**

- Klasyczna szkoła, asercje stanowe TUnit (`Assert.That` + `Assert.Multiple`). Nazwy `Method_Scenario_Expected`.
- Kontrakt screenshot: pełne pokrycie `ScreenshotArgs.ParseAndRemove` i dwóch guardów opcji — publiczne API.
- Logging: `HostLogArgsParser` (`internal`) + `NestedProgressTracker` (`internal`) + jeden test `HostLog.Open` (tylko obcięcie argv). Brak testu sinków console / file / named-pipe / runner.
- Avalonia: tylko `VisualScreenshotCapture.CaptureAsync` (flatten alpha). Brak `AttachScreenshot` / `AvaloniaScreenshot` (główne okno).

## Assessment summary

| ID | Priority | Topic | Location |
|----|----------|-------|----------|
| F-01 | P2 | Brak testów paczki WinUI 3; Avalonia bez capture głównego okna | `DevKit.Screenshot.WinUi3.Sharp/` (brak `*.Tests`); `VisualScreenshotCaptureTests.cs` |
| F-02 | P2 | Obserwowalne sinki Logging niepokryte | `HostLogArgsParserTests.cs`; `FileStartupSession.cs`; `DevKit.Logging.Runner` |
| F-03 | P2 | CI nie publikuje wyników testów; trzy luźne `dotnet run` | `.github/workflows/ci.yml` |
| F-04 | P3 | Dziewięć analogicznych `[Test]` parsera zamiast parametrów | `ScreenshotArgsTests.cs` |

Brak P0/P1: testy **są** odpalane w CI i lokalnie przechodzą.

## Findings

### F-01: Shipping adapter WinUI 3 bez siatki testów; Avalonia pokrywa tylko flatten elementu

**Co:** `DevKit.Screenshot.WinUi3.Sharp` jest `IsPackable=true` i leży na NuGet (0.1.2), a w `tests/` i w `DevKit.Sharp.slnx` nie ma projektu testowego. CI (`.github/workflows/ci.yml` linie 38–42) odpala trzy hosty — żaden nie ładuje WinUI. W WinUI leżą m.in. `BgraPixelFlattener`, `PngEncoder`, `ElementScreenshotCapture`, `WinUiScreenshot` — zero asercji. Avalonia ma trzy testy headless wyłącznie na `VisualScreenshotCapture.CaptureAsync` (alfa 255 / 0 / czerwone tło); `AvaloniaScreenshot` / `AttachScreenshot` nie są wołane.

**Jak:** (1) Projekt `tests/DevKit.Screenshot.WinUi3.Sharp.Tests` z TUnit, TFM `net10.0-windows10.0.19041.0`. Na start testy bez okna: `BgraPixelFlattener.FlattenOntoBackground` (wejście BGRA + `Windows.UI.Color`). (2) Capture okna/elementu — gdy pojawi się harness (WinAppSDK test host albo wąski sample pod CI). (3) W Avalonia dodać headless test `IScreenshot` / `AttachScreenshot` na `Window`, analogicznie do istniejącej sesji `HeadlessUnitTestSession`.

**Dlaczego:** Ryzyko produktu siedzi na granicy IO/UI, nie w parserze CLI. Khorikov ch. 8 § 8.1 / 8.2 — integracja tam, gdzie zależność out-of-process (tu: renderer / AppWindow): [../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/08-why-integration-testing.md](../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/08-why-integration-testing.md). Karta: [../../../../audits/xunit-testing/compendium/references/test-strategy.md](../../../../audits/xunit-testing/compendium/references/test-strategy.md) — „No integration tests where IO boundary is the risk | P2”. Pillar *protection against regressions* (ch. 4 § 4.1.1): [../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/04-the-four-pillars-of-a-good-unit-test.md](../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/04-the-four-pillars-of-a-good-unit-test.md). Cap P2 — to luka strategii, nie ślepe CI (Avalonia/kontrakt jednak biegają). WinUI headless jest droższy niż Avalonia; dlatego fala 1 = flatten/PNG bez okna.

### F-02: Logging testuje parser i tracker, nie sinki które są produktem

**Co:** `DevKit.Logging.Sharp` sprzedaje raport startu: console (alokacja na żądanie), plik, detached runner po named pipe. Testy: 4× `HostLogArgsParser.ParseAndRemove` (`internal`), 3× `NestedProgressTracker` (`internal`), 1× `HostLog.Open` sprawdzający tylko `IsEnabled` i obcięcie argv. `FileStartupSession` jest `public` i nie ma testu. `DevKit.Logging.Runner` (host produkcyjny) nie ma projektu testowego. CI nie odpala samples.

**Jak:** Test `HostLog.Open(ref args)` z `--devkit-logging file <temp>` i asercją na treść pliku (timestamp + linia). Opcjonalnie cienki test pipe + runner jako nightly, nie na każdy PR.

**Dlaczego:** Dla biblioteki logowania log **jest** obserwowalnym zachowaniem, nie detalem implementacji — Khorikov ch. 8 § 8.6.1 („if you write a logging library, then the logs this library produces are the most important… part of its observable behavior”): [../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/08-why-integration-testing.md](../../../../audits/xunit-testing/compendium/books/khorikov-unit-testing-ppp/chapters/08-why-integration-testing.md). Tracker jako wydzielona kalkulacja to dobry humble object (ch. 7) — zostawić. Cap P2 (book-only / strategy, [../../../../audits/xunit-testing/compendium/references/severity-model.md](../../../../audits/xunit-testing/compendium/references/severity-model.md)).

### F-03: CI nie zostawia artefaktów testów; trzy osobne `dotnet run`

**Co:** Job Test w `.github/workflows/ci.yml` woła trzy `dotnet run --project … --no-build` i kończy się packiem. Brak `--report-junit` / TRX, brak `actions/upload-artifact` na wyniki, brak `dotnet test` na `.slnx`. `TestingPlatformDotnetTestSupport` jest włączone, więc `dotnet test` mógłby zbierać exit code z trzech exe.

**Jak:** Jeden krok `dotnet test DevKit.Sharp.slnx -c Release --no-build` (MTP) z reporterem JUnit/TRX i uploadem `if: always()`. Zachować `dotnet run` lokalnie w README, jeśli to wygodniejszy UX TUnit.

**Dlaczego:** Topologia CI bez structured results = brak trendu i ślepy PR check poza logiem — [../../../../audits/xunit-testing/compendium/references/ci-and-topology.md](../../../../audits/xunit-testing/compendium/references/ci-and-topology.md) „CI publishes no structured test results | P2”. Format XML (wzorzec, nie xUnit-runner w tym repo): [../../../../audits/xunit-testing/compendium/oss-curated/xunit-docs/docs/format-xml-v2.md](../../../../audits/xunit-testing/compendium/oss-curated/xunit-docs/docs/format-xml-v2.md). MTP CLI mapuje reportery (`--report-junit`): [../../../../audits/xunit-testing/compendium/oss-curated/xunit-docs/docs/getting-started/v3/microsoft-testing-platform.md](../../../../audits/xunit-testing/compendium/oss-curated/xunit-docs/docs/getting-started/v3/microsoft-testing-platform.md) (wzmianka TUnit jako innego hosta MTP). GAPS: kompendium nie ma karty GitHub Actions — ten sam partial co wcześniejsze runy. Cap P2.

### F-04: Parser screenshotów — kopia `[Test]` zamiast parametrów

**Co:** `ScreenshotArgsTests` ma ~9 metod `ParseAndRemove_*` różniących się tablicą argv i oczekiwaniami. TUnit ma `[Arguments]` / data source; tu same osobne `[Test]`.

**Jak:** Przy następnym tykaniu parsera zwinąć przypadki przełączników do jednej metody z danymi. Guardy `EnsureEnabled` / `RequireOutputPath` zostawić osobno (inny akt).

**Dlaczego:** Khorikov ch. 3 § 3.5 (parameterized) i karta [../../../../audits/xunit-testing/compendium/references/refactoring-tests.md](../../../../audits/xunit-testing/compendium/references/refactoring-tests.md) „Large duplicate Facts differing only by input | P2–P3”. Przy 12 testach w jednym pliku — P3.

## To-Be Test Layout

Zostać przy **TUnit + split per paczka** (już jest). Nie scalać w monolit, nie wprowadzać xUnit.

```text
tests/
  DevKit.Logging.Sharp.Tests/                 # parser + file sink (nowe) + tracker
  DevKit.Screenshot.Sharp.Tests/              # CLI contract (jest)
  DevKit.Screenshot.Avalonia.Sharp.Tests/     # element + main-window headless
  DevKit.Screenshot.WinUi3.Sharp.Tests/       # fala 1: flatten; fala 2: capture gdy harness
```

CI:

| Etap | Co |
|------|----|
| PR / `main` | `dotnet test DevKit.Sharp.slnx -c Release` + JUnit/TRX artifact; bez filtra (suite ~2 s wall na projekt) |
| Nightly (opcjonalnie) | sample `--devkit-screenshot-exit` / runner pipe — gdy pojawi się F-01/F-02 fala 2 |

Trait `Category=` nie jest potrzebny, dopóki nie ma wolnych testów WinUI/runner. Split projektów **jest** taksonomią.

## Not flagged

- **TUnit zamiast xUnit** — świadomy wybór MTP; mechanika xUnit N/A; bez finding „migruj na v3”.
- **Suite &lt; 1 s silnika / &lt; 5 s wall na projekt** — brak split unit/integration w CI.
- **`HeadlessUnitTestSession` static** — izolacja `PerTest` z Avalonia; nie shared domain state.
- **`InternalsVisibleTo` na parser/tracker** — CLI i pasek postępu to kontrakt produktu; tracker to wydzielona kalkulacja (humble object), nie test prywatnej metody przez refleksję.
- **Brak mocków** — classical, output/state-based; nic do weryfikowania call sequence.
- **Samples nie są testami** — `samples/` to dema, nie gate.
- **`Microsoft.NET.Test.Sdk` obok TUnit** — routed (poniżej).

## Routed to sibling audits

| Sygnał | Pakiet |
|--------|--------|
| `TestingPlatformDotnetTestSupport` + `Microsoft.NET.Test.Sdk` + brak `test.runner` w `global.json`; TFM testów tylko `net10.0` przy multi-target bibliotek | `modern-dotnet-architecture-review` |
| Krawędzie `ProjectReference` test→prod, brak testów WinUI jako węzeł grafu | `transitive-references-review` (ten sam dzień) |
| Testowalność `WinUiScreenshot` (reflection `m_window`) jako szew produkcyjny | `object-design-review` |
| `IServiceCollection.AddScreenshot` w testach | `di-architecture-review` — nie dotyczy, DI nie jest hostowane w testach |

## Calibration

Parser `ScreenshotArgs` to wzorcowy test kontraktu CLI: publiczne API, asercja na wynik **i** na `ref args`, nazwy scenariuszy, zero mocków, 12/12 w 181 ms. Avalonia headless z `UseHeadlessDrawing = false` realnie sprawdza piksele (alfa) — to nie jest test-atrapa. CI **w ogóle odpala** testy na każdym PR (w przeciwieństwie do finding „CI never runs tests”).
