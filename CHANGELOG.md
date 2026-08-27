# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
on the **0.x** line (see [Versioning](README.md#versioning) in the root README).
Each packable project has its own `<Version>`; a git tag `v*` is a release marker,
not a family pin.

## [Unreleased]

### Added

- `DevKit.Focus.Sharp`, `DevKit.Focus.Avalonia.Sharp`, and `DevKit.Focus.WinUi3.Sharp`
  (keyboard-focus policy + Avalonia 12 / WinUI 3 adapters) live in this monorepo.
- One TUnit test project per Focus package (contract, headless Avalonia tree, WinUI STA).

### Changed

- Package versions are independent (`0.3.0` in each packable csproj). Raising a UI-library
  floor no longer locksteps Logging or other tools.
- WinUI adapters require Windows App SDK **2.4.0** (MINOR of those adapters).
- Avalonia adapters require Avalonia **12.1.1**.
- Screenshot UI adapters: `Microsoft.Extensions.DependencyInjection.Abstractions`
  **8.0.2** (`net8`) / **10.0.11** (`net10`).
- Shared MSBuild pins for Avalonia, Windows App SDK, BuildTools, TUnit, and Test SDK.
- Screenshot WinUI 3 has a TUnit project; CI runs all seven test hosts.

These package versions are **not on NuGet yet**. Last published line is **0.1.2**.

## [0.1.2] - 2026-08-03

### Changed

- Element capture flattens transparency by default so clipboard and file consumers that
  do not composite PNG alpha still show content.

## [0.1.1] - 2026-08-02

### Added

- Public subtree / element capture: `VisualScreenshotCapture` (Avalonia) and
  `ElementScreenshotCapture` (WinUI 3).

### Changed

- SourceLink enabled on packable projects.

## [0.1.0] - 2026-08-02

First public release under the **DevKit.Sharp** name.

### Added

- `DevKit.Screenshot.Sharp` contract and `--devkit-screenshot` CLI parser.
- `DevKit.Screenshot.Avalonia.Sharp` and `DevKit.Screenshot.WinUi3.Sharp` main-window capture.
- `DevKit.Logging.Sharp` startup reporter (`--devkit-logging`).
