# Przegląd referencji tranzytywnych — DevTools.Sharp

**Data:** 2026-08-27
**Baseline:** `main` (`3522e36`)
**Status:** przegląd — doradczy, chyba że oznaczono jako wiążący
**Rozwiązanie:** `DevKit.Sharp.slnx` (1 plik)
**Compendium:** `../../../../audits/transitive-references/` (cytaty względne wobec tego folderu runu)

**Parametry audytu**

| Parametr | Wartość |
|----------|---------|
| `AUDIT_HOME` | `C:\Users\newon\source\repos\audits\transitive-references` |
| `AUDIT_TARGET` | `C:\Users\newon\source\repos\DevTools.Sharp` |
| Wykluczenia grafu | `bin/`, `obj/`, `.claude/`, `references/` |
| Korpus produkcyjny | katalogi `DevKit.*` w root (paczki + `DevKit.Logging.Runner`) |
| Korpus testów | `tests/**` (nazwa `*.Tests`) |
| Korpus samples | `samples/**` |
| Projekty kluczowe | 4× `IsPackable=true` + host `DevKit.Logging.Runner` |

**Phase 0 — prior runs:** brak `audits/*-transitive-references`. To **pierwszy pomiar** — tabela metryk jest baseline’em trendu.

---

## 1. Metryki

**Korpus:** 16 projektów (5 produkcyjnych, 3 testy, 8 samples). Wszystkie 16 na dysku = w `DevKit.Sharp.slnx` (brak driftu membership).

**Binding checks**

| Check | Wynik |
|-------|-------|
| Cykle w produkcji | **brak** |
| Krawędzie produkcja → test/spike | **brak** |
| Gate zestawu packable vs drzewo | **zgodne** — 4× `IsPackable=true` = cztery nupkgi z `dotnet pack` / `publish.yml`; `Directory.Build.props` default `IsPackable=false`, opt-in na paczkach |

**Tabela zamknięć (projekty kluczowe, korpus produkcyjny)**

| Projekt | refs bezpośr. | zamknięcie tranzytywne | max głębokość | Δ vs prior |
|---------|---------------|------------------------|---------------|------------|
| `DevKit.Screenshot.Sharp/DevKit.Screenshot.Sharp.csproj` | 0 | 0 | 0 | — (baseline) |
| `DevKit.Logging.Sharp/DevKit.Logging.Sharp.csproj` | 0 | 0 | 0 | — |
| `DevKit.Screenshot.Avalonia.Sharp/DevKit.Screenshot.Avalonia.Sharp.csproj` | 1 | 1 | 1 | — |
| `DevKit.Screenshot.WinUi3.Sharp/DevKit.Screenshot.WinUi3.Sharp.csproj` | 1 | 1 | 1 | — |
| `DevKit.Logging.Runner/DevKit.Logging.Runner.csproj` | 1 | 1 | 1 | — |

Definicje: [../../../../audits/transitive-references/compendium/references/graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md) § Metrics (`len(DFS(P)) − 1`, longest path). Brak assetu advisory-test w repo (szablon skillu jest xUnit — GAPS).

**Huby (przychodzące, produkcja)**

| Projekt | przychodzące (produkcja) |
|---------|--------------------------|
| `DevKit.Screenshot.Sharp` | 2 (Avalonia.Sharp, WinUi3.Sharp) |
| `DevKit.Logging.Sharp` | 1 (Logging.Runner) |

Z testami/samples: Screenshot.Sharp = 4 (2 prod + `Sample.ViewModels` + `*.Tests`); Logging.Sharp = 4 (runner + 2 sample + tests); Screenshot.Avalonia.Sharp = 3 (Views, App, tests); `Sample.ViewModels` = 4 (oba Views + oba App).

**Liście produkcyjne (0 ProjectReference):** `DevKit.Screenshot.Sharp`, `DevKit.Logging.Sharp`.

**Krawędzie redundantne w produkcji:** 0 (każdy projekt produkcyjny ma 0 albo 1 ref).

---

## 2. As-Is Reference Graph (fakty)

**Warstwa obserwowana:**

```text
HOST              DevKit.Logging.Runner          (Exe, AOT, nie packable)
  ↑
FEATURES / UI     Screenshot.Avalonia.Sharp
                  Screenshot.WinUi3.Sharp        (równoległe adaptery; brak krawędzi między nimi)
  ↑
CONTRACTS         Screenshot.Sharp               (liść, 0 refs, 0 PackageReference)
                  Logging.Sharp                  (liść, 0 ProjectReference; pakiety tylko TFM BCL)
```

Tests wiszą z boku (→ jedna paczka). Samples wyłączone z metryk produkcyjnych.

**Pełna lista krawędzi produkcyjnych (3):**

| From | To |
|------|----|
| `DevKit.Screenshot.Avalonia.Sharp` | `DevKit.Screenshot.Sharp` |
| `DevKit.Screenshot.WinUi3.Sharp` | `DevKit.Screenshot.Sharp` |
| `DevKit.Logging.Runner` | `DevKit.Logging.Sharp` |

**Set coherence**

- `Directory.Build.props` **nie** wymusza `AssemblyName` / `RootNamespace` z `$(MSBuildProjectName)`. Paczki DevKit polegają na default = nazwa projektu; zgadza się z `namespace DevKit.*.Sharp`.
- Samples nadpisują `RootNamespace`/`AssemblyName` (Avalonia.App, WinUi3.App, …).
- Split namespace: `samples/WinUi3.App/Views/MainWindow.xaml.cs` deklaruje `namespace WinUi3.Views` w assembly `WinUi3.App` (obok projektu `WinUi3.Views`). Polyfille `System.Threading` / `System.Runtime.CompilerServices` w bibliotekach — nie liczone jako split produktu.
- Guard już istnieje: `eng/verify-contract-boundary.py` (CI) — `DevKit.Screenshot.Sharp` nie może mieć `ProjectReference`/`PackageReference`/`FrameworkReference` ani tokenów UI w `.cs`; `DevKit.Logging.Sharp` nie może mieć UI w zależnościach. To ratchet nazwany, nie metryka „depth ≤ N”.
- Brak testów architektury (`Category=architecture`).

**PackageReference — drift wersji (raport, bez fixa)**

Ten sam pakiet, ta sama wersja w grafie, z wyjątkiem **świadomego** rozszczepienia TFM:

| Pakiet | Gdzie | Wersje |
|--------|-------|--------|
| Avalonia | adapter Avalonia + tests + samples | 12.1.0 wszędzie |
| Microsoft.WindowsAppSDK | WinUI adapter + WinUI samples | 2.2.0 |
| Microsoft.Windows.SDK.BuildTools | WinUI (prod `PrivateAssets=all`) + samples | 10.0.26100.4654 |
| Microsoft.Extensions.DependencyInjection.Abstractions | Avalonia/WinUI adapters | 8.0.2 (`net8.0*`) / 10.0.6 (`net10.0*`) — warunek TFM, nie drift |
| Microsoft.Extensions.DependencyInjection | samples App | 10.0.6 (inny id niż Abstractions) |
| TUnit / Microsoft.NET.Test.Sdk | 3 testy | 1.13.11 / 17.14.1 |
| Microsoft.SourceLink.GitHub | packable via Directory.Build.props | 10.0.301, `PrivateAssets=all` |

Brak „ten sam PackageReference, dwie wersje na tym samym TFM”.

---

## 2. Findings (ordered by consequence)

Brak P0/P1 (cykl, prod→test, ślepy pack gate). Jedyny finding to P3 naming w samples.

### F-1 — Namespace `WinUi3.Views` w assembly `WinUi3.App` (P3, layout)

**Co:** `samples/WinUi3.App/Views/MainWindow.xaml.cs` jest w projekcie `WinUi3.App` (`RootNamespace`/`AssemblyName` = `WinUi3.App`), a plik otwiera `namespace WinUi3.Views`. Równoległy projekt `samples/WinUi3.Views` też własny ten namespace. Avalonia trzyma `MainWindow` w `Avalonia.Views`; WinUI włożył okno do App.

**Jak:** Przy następnym tykaniu sample WinUI — przenieść `Views/MainWindow.xaml(+.cs)` do `WinUi3.Views` (jak Avalonia) **albo** zmienić namespace na `WinUi3.App`. Nie robić sweepu.

**Dlaczego:** Naming alignment cap P3 — [../../../../audits/transitive-references/compendium/references/decision-principles.md](../../../../audits/transitive-references/compendium/references/decision-principles.md) § Naming alignment; pułapka `namespace → assembly` w [../../../../audits/transitive-references/compendium/references/graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md) § False-positive traps. Korpus samples — de-escalate względem produkcji ([../../../../audits/transitive-references/compendium/references/severity-model.md](../../../../audits/transitive-references/compendium/references/severity-model.md) rule 3).

---

## 3. Stale-edge sweep

Mapa `namespace → assembly` z deklaracji `namespace` w `.cs` (produkcja): `DevKit.Screenshot.Sharp`, `DevKit.Screenshot.Avalonia.Sharp`, `DevKit.Screenshot.WinUi3.Sharp`, `DevKit.Logging.Sharp` (+ `.Progress`), `DevKit.Logging.Runner` (top-level statements, brak namespace).

Skan użycia `using` / FQN w źródłach produkcyjnych: **zero kandydatów**. Każda z trzech krawędzi importuje typy liścia (`IScreenshot` / `ScreenshotOptions` / `ScreenshotArgs`; `StartupConsoleView` / `HostLog` / `StartupWireMessage`).

| referencing project | removed reference | verification | note |
|---|---|---|---|
| — | — | nie usuwano krawędzi | skan nie dał kandydata; kompilator nie był potrzebny jako arbiter |

Unverified (samples, poza protokołem stale-prod): krawędzie `Avalonia.App` → `Screenshot.Avalonia.Sharp` oraz `→ Sample.ViewModels` są **redundantne** względem `Avalonia.Views`, ale **używane** w `App.axaml.cs` (`AttachScreenshot`, `AddScreenshot`, `MainWindowViewModel`). Analogicznie `WinUi3.App`. To redundant-but-used, nie stale ([graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md) § Redundant ≠ stale).

---

## 4. To-Be reference layout (proposal)

As-Is **już jest** docelowym DAG z karty [../../../../audits/transitive-references/compendium/references/optimal-layout.md](../../../../audits/transitive-references/compendium/references/optimal-layout.md): dwa liście kontraktów, adaptery UI równolegle (żaden nie sięga w drugi framework), host runner tylko nad Logging, composition w samples (nie w paczkach). Convenience (`AddScreenshot`) żyje w adapterze — konsument może złożyć `IScreenshot` ręcznie.

Edge changes:

| action | edge | wave | status |
|---|---|---|---|
| keep | Avalonia.Sharp → Screenshot.Sharp | — | używane |
| keep | WinUi3.Sharp → Screenshot.Sharp | — | używane |
| keep | Logging.Runner → Logging.Sharp | — | używane |
| keep (deliberate) | Avalonia.App → Screenshot.Avalonia.Sharp (redundant vs Views) | — | App woła `AttachScreenshot` |
| keep (deliberate) | WinUi3.App → Screenshot.WinUi3.Sharp | — | composition root sample |
| drop-down | — | — | brak kandydata hub→leaf w produkcji |
| move (sample) | `WinUi3.App/Views/MainWindow` → `WinUi3.Views` **lub** namespace `WinUi3.App` | 2 (gdy tykanie WinUI sample) | F-1 |

Named exceptions: głębokość hosta Runner = 1 jest rolą, nie zapachem ([decision-principles.md](../../../../audits/transitive-references/compendium/references/decision-principles.md) § no global maximum depth). `Logging.Runner` niepackable przy packable `Logging.Sharp` — exe towarzyszący, nie nupkg; nie jest ślepym gate’em.

---

## 5. Suggested guards (conservative)

**Binding (już jest, nie dodawać duplikatu):** `eng/verify-contract-boundary.py` — Screenshot.Sharp zero refs + zakaz tokenów UI; Logging.Sharp zakaz UI w Package/Project/FrameworkReference. Ratchet nazwany dwóch projektów, red na naruszeniu tokenu. CI odpala to przed restore.

**Binding nowe:** żadne. Nie proponować „depth ≤ N” ani „Avalonia nie referencjonuje WinUI” jako testu — druga jest fizycznie prawdziwa i nie ma ścieżki regresji bez nowej krawędzi, którą review i tak zobaczy na 16 projektach.

**Advisory (opt-in):** skopiować `ProjectReferenceGraph.cs` + test metryk **tylko jeśli** graf urośnie (kolejny adapter / host). Szablon skillu jest xUnit; to repo jest TUnit — nie wstawiać assetu 1:1 (GAPS).

---

## 6. Explicitly not flagged

- Dwa liście kontraktów bez `ProjectReference` — to cel, nie luka.
- Adapter Avalonia i WinUI oba → ten sam kontrakt, zero krawędzi między sobą — zamierzone rozdzielenie decyzji UI.
- `Logging.Runner` poza zestawem nupkg — host, nie biblioteka.
- Redundant App → Views + App → adapter/VM w samples — dokumentacja bezpośredniej zależności composition root.
- `Sample.ViewModels` → `Screenshot.Sharp` (kontrakt, nie adapter) — VM nie ciągnie Avalonia/WinUI.
- `WinUi3.Views` **nie** referencjonuje `Screenshot.WinUi3.Sharp` (okno siedzi w App) — niesymetria vs Avalonia.Views, świadoma przy obecnym rozłożeniu plików; nie stale.
- Brak `Tests.Architecture` — przy 3 krawędziach produkcyjnych overkill.

---

## 7. Correctly simple

`DevKit.Screenshot.Sharp` jest prawdziwym liściem: **zero** `ProjectReference` i **zero** `PackageReference`, a `verify-contract-boundary.py` to egzekwuje na każdym PR. To najcenniejszy kształt z [optimal-layout.md](../../../../audits/transitive-references/compendium/references/optimal-layout.md) § true leaf. Druga kalibracja: DAG 3 krawędzie, depth 1 na adapterach — host głęboki nie istnieje, bo nie ma warstwy composition w paczkach.

---

## 8. Referat: Co, Jak, Dlaczego

Graf jest mały i już warstwowy. Audyt tego typu na 16 projektach służy **baseline’owi**: następny przebieg porównuje tabelę zamknięć (Screenshot.Sharp musi zostać przy 0/0/0; jeśli adapter dostanie drugi `ProjectReference`, zamknięcie 1→2 jest sygnałem).

F-1 jest jedynym odchyleniem i siedzi w sample WinUI — koszt taxonomii namespace/assembly dla przyszłych skanów, nie koszt buildu. Stale-edge protocol ([review-procedure.md](../../../../audits/transitive-references/compendium/references/review-procedure.md) § 5) nie usunął krawędzi, bo skan nie wyprodukował hipotezy; kompilator jako arbiter nie był wołany.

Postawa: advisory. Jedyny binding check (cykle) jest czysty; jedyny wart utrzymania ratchet to istniejący skrypt granicy kontraktu.

---

## Appendix

**Krawędzie testów (nie produkcja)**

| From | To |
|------|----|
| `tests/DevKit.Logging.Sharp.Tests` | `DevKit.Logging.Sharp` |
| `tests/DevKit.Screenshot.Sharp.Tests` | `DevKit.Screenshot.Sharp` |
| `tests/DevKit.Screenshot.Avalonia.Sharp.Tests` | `DevKit.Screenshot.Avalonia.Sharp` |

Brak projektu testowego WinUI — to luka **test strategy** (raport xunit-testing z tego samego dnia), nie krawędź grafu do usunięcia.

**Krawędzie samples**

| From | To |
|------|----|
| `samples/Logging.Sample.Console` | `DevKit.Logging.Sharp` |
| `samples/Logging.Sample.Gui` | `DevKit.Logging.Sharp` |
| `samples/Sample.ViewModels` | `DevKit.Screenshot.Sharp` |
| `samples/Avalonia.Views` | `DevKit.Screenshot.Avalonia.Sharp`, `Sample.ViewModels` |
| `samples/Avalonia.App` | `DevKit.Screenshot.Avalonia.Sharp`, `Avalonia.Views`, `Sample.ViewModels` |
| `samples/WinUi3.Views` | `Sample.ViewModels` |
| `samples/WinUi3.App` | `DevKit.Screenshot.WinUi3.Sharp`, `Sample.ViewModels`, `WinUi3.Views` |

**Trend:** n/a — pierwszy run.
