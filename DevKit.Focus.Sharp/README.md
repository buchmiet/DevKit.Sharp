# DevKit.Focus.Sharp

Framework-neutral keyboard-focus policy for .NET desktop applications.

The package models a **focus scope** rather than a framework control. The application
chooses the active scope; `KeyboardFocusCoordinator` only verifies or restores keyboard
focus inside it.

Pair it with:

- `DevKit.Focus.Avalonia.Sharp`
- `DevKit.Focus.WinUi3.Sharp`

## Core usage

```csharp
var coordinator = new KeyboardFocusCoordinator(result =>
    logger.LogDebug("Focus {Scope}: {Outcome}", result.ScopeId, result.Outcome));

coordinator.Apply(
    currentSurface.FocusScope,
    new KeyboardFocusRequest(
        KeyboardFocusReason.SurfaceEntered,
        KeyboardFocusMode.Ensure,
        "Panels -> Terminal"));
```

The coordinator does not own application mode, inspect a visual tree, use a dispatcher,
or depend on a UI framework.

## Composite scopes

```csharp
var panels = new CompositeKeyboardFocusScope(
    "panels",
    defaultScope: () => activePanel.FocusScope,
    members: () => new[]
    {
        leftPanel.FocusScope,
        rightPanel.FocusScope,
        commandLine.FocusScope,
    });
```

Focus in any member is valid. Forced navigation still sends focus to the current default scope.

## Ownership

- `Component` — the component may focus itself during initialization.
- `Host` — the host is the only owner of initial/surface focus.

This library targets `netstandard2.0` and `net8.0`. It has no UI or DI package dependencies.

## License

MIT
