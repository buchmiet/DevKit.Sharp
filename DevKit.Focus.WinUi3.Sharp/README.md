# DevKit.Focus.WinUi3.Sharp

WinUI 3 adapter for `DevKit.Focus.Sharp`.

It supplies:

- focus scopes backed by a WinUI visual tree and `XamlRoot`;
- a fluent builder for component roots, default targets, owned popups, and availability;
- explicit `DispatcherQueue` scheduling;
- first-activation and reactivation focus restoration.

## Simple component

```csharp
public sealed class TerminalView : FrameworkElement, IKeyboardFocusSurface
{
    public TerminalView()
    {
        IsTabStop = true;
        FocusScope = this.CreateKeyboardFocusScope("terminal");
    }

    public IKeyboardFocusScope FocusScope { get; }
}
```

## Editor scope

```csharp
FocusScope = WinUi3KeyboardFocusScope
    .For(EditorRoot)
    .Named("editor")
    .Default(EditorControl)
    .Include(FindPopupRoot)
    .AvailableWhen(() => EditorRoot.Visibility == Visibility.Visible)
    .Build();
```

Each included root is inspected through its own `XamlRoot`, so owned popup/flyout trees
can remain valid members of the logical scope.

## Host integration

```csharp
var coordinator = new KeyboardFocusCoordinator(LogFocus);

using var restoration = window.AttachKeyboardFocusRestoration(
    coordinator,
    () => currentSurface?.FocusScope);

coordinator.PostFocus(
    window.DispatcherQueue,
    terminal.FocusScope,
    new KeyboardFocusRequest(
        KeyboardFocusReason.SurfaceEntered,
        detail: "Panels -> Terminal"));
```

Requires Windows 10 17763+, .NET 8 or .NET 10, and Windows App SDK 2.4+.

## License

MIT
