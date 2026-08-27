# DevKit.Focus.Avalonia.Sharp

Avalonia 12 adapter for `DevKit.Focus.Sharp`.

It supplies:

- focus scopes backed by an Avalonia visual tree;
- a fluent builder for component roots, default targets, owned popups, and availability;
- UI-thread scheduling at input priority;
- window activation focus restoration.

## Simple component

```csharp
public partial class TerminalView : UserControl, IKeyboardFocusSurface
{
    public TerminalView()
    {
        InitializeComponent();
        FocusScope = this.CreateKeyboardFocusScope("terminal");
    }

    public IKeyboardFocusScope FocusScope { get; }
}
```

## Editor scope

```csharp
FocusScope = AvaloniaKeyboardFocusScope
    .For(this)
    .Named("editor")
    .Default(Editor)
    .Include(FindPopup)
    .AvailableWhen(() => IsVisible)
    .Build();
```

Focus in any descendant of the editor root or included visual is treated as valid.
`TryFocusDefault()` still targets `Editor`.

## Host integration

```csharp
var coordinator = new KeyboardFocusCoordinator(LogFocus);

using var restoration = window.AttachKeyboardFocusRestoration(
    coordinator,
    () => currentSurface?.FocusScope);

coordinator.PostFocus(
    terminal.FocusScope,
    new KeyboardFocusRequest(
        KeyboardFocusReason.SurfaceEntered,
        detail: "Panels -> Terminal"));
```

Requires Avalonia 12.1.1+.

## License

MIT
