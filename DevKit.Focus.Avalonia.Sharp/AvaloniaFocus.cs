using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DevKit.Focus.Sharp;

namespace DevKit.Focus.Avalonia.Sharp;

/// <summary>Creates Avalonia-backed keyboard focus scopes.</summary>
public static class AvaloniaKeyboardFocusScope
{
    public static AvaloniaFocusScopeBuilder For(Control root)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        return new AvaloniaFocusScopeBuilder(root);
    }
}

/// <summary>Fluent builder for an Avalonia visual-tree focus scope.</summary>
public sealed class AvaloniaFocusScopeBuilder
{
    private readonly Control _root;
    private readonly List<Visual> _additionalRoots = new();
    private string? _id;
    private Func<Control?>? _defaultTarget;
    private Func<bool>? _availableWhen;

    internal AvaloniaFocusScopeBuilder(Control root)
    {
        _root = root;
    }

    public AvaloniaFocusScopeBuilder Named(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A focus scope id is required.", nameof(id));

        _id = id;
        return this;
    }

    public AvaloniaFocusScopeBuilder Default(Control target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        _defaultTarget = () => target;
        return this;
    }

    public AvaloniaFocusScopeBuilder Default(Func<Control?> targetResolver)
    {
        _defaultTarget = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        return this;
    }

    public AvaloniaFocusScopeBuilder Include(Visual additionalRoot)
    {
        if (additionalRoot is null)
            throw new ArgumentNullException(nameof(additionalRoot));

        _additionalRoots.Add(additionalRoot);
        return this;
    }

    public AvaloniaFocusScopeBuilder AvailableWhen(Func<bool> predicate)
    {
        _availableWhen = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    public IKeyboardFocusScope Build()
    {
        var id = _id;
        if (string.IsNullOrWhiteSpace(id))
        {
            id = string.IsNullOrWhiteSpace(_root.Name)
                ? _root.GetType().Name
                : _root.Name;
        }

        return new AvaloniaControlFocusScope(
            id!,
            _root,
            _defaultTarget ?? (() => _root),
            _additionalRoots.ToArray(),
            _availableWhen);
    }
}

internal sealed class AvaloniaControlFocusScope : IKeyboardFocusScope
{
    private readonly Control _root;
    private readonly Func<Control?> _defaultTarget;
    private readonly IReadOnlyList<Visual> _additionalRoots;
    private readonly Func<bool>? _availableWhen;

    public AvaloniaControlFocusScope(
        string id,
        Control root,
        Func<Control?> defaultTarget,
        IReadOnlyList<Visual> additionalRoots,
        Func<bool>? availableWhen)
    {
        Id = id;
        _root = root;
        _defaultTarget = defaultTarget;
        _additionalRoots = additionalRoots;
        _availableWhen = availableWhen;
    }

    public string Id { get; }

    public bool CanReceiveFocus => TryResolveAvailableTarget(out _);

    public bool ContainsKeyboardFocus
    {
        get
        {
            if (ContainsFocus(_root))
                return true;

            foreach (var root in _additionalRoots)
            {
                if (ContainsFocus(root))
                    return true;
            }

            return false;
        }
    }

    public bool TryFocusDefault()
    {
        return TryResolveAvailableTarget(out var target) && target!.Focus();
    }

    private bool TryResolveAvailableTarget(out Control? target)
    {
        target = null;

        if (_availableWhen is not null && !_availableWhen())
            return false;

        if (!_root.IsVisible || !_root.IsEffectivelyEnabled || TopLevel.GetTopLevel(_root) is null)
            return false;

        target = _defaultTarget();
        if (target is null || !target.IsVisible || !target.IsEffectivelyEnabled)
            return false;

        return TopLevel.GetTopLevel(target) is not null;
    }

    private static bool ContainsFocus(Visual root)
    {
        var topLevel = TopLevel.GetTopLevel(root);
        var focused = topLevel?.FocusManager?.GetFocusedElement();
        if (focused is not Visual focusedVisual)
            return false;

        if (ReferenceEquals(focusedVisual, root))
            return true;

        return focusedVisual.GetVisualAncestors().Any(ancestor => ReferenceEquals(ancestor, root));
    }
}

/// <summary>Convenience and host-lifetime helpers for Avalonia applications.</summary>
public static class AvaloniaKeyboardFocusExtensions
{
    public static IKeyboardFocusScope CreateKeyboardFocusScope(
        this Control root,
        string? id = null,
        Control? defaultTarget = null,
        Func<bool>? availableWhen = null)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        var builder = AvaloniaKeyboardFocusScope.For(root);
        if (!string.IsNullOrWhiteSpace(id))
            builder.Named(id!);
        if (defaultTarget is not null)
            builder.Default(defaultTarget);
        if (availableWhen is not null)
            builder.AvailableWhen(availableWhen);
        return builder.Build();
    }

    /// <summary>Schedules one focus request on Avalonia's UI thread at input priority.</summary>
    public static void PostFocus(
        this KeyboardFocusCoordinator coordinator,
        IKeyboardFocusScope scope,
        KeyboardFocusRequest request)
    {
        PostFocus(coordinator, scope, request, DispatcherPriority.Input);
    }

    /// <summary>Schedules one focus request on Avalonia's UI thread.</summary>
    public static void PostFocus(
        this KeyboardFocusCoordinator coordinator,
        IKeyboardFocusScope scope,
        KeyboardFocusRequest request,
        DispatcherPriority priority)
    {
        if (coordinator is null)
            throw new ArgumentNullException(nameof(coordinator));
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        Dispatcher.UIThread.Post(() => coordinator.Apply(scope, request), priority);
    }

    /// <summary>
    /// Restores focus for the currently selected scope whenever the window becomes active. The first
    /// activation is reported as <see cref="KeyboardFocusReason.InitialWindowOpened"/>; later ones as
    /// <see cref="KeyboardFocusReason.WindowReactivated"/>.
    /// </summary>
    public static IDisposable AttachKeyboardFocusRestoration(
        this Window window,
        KeyboardFocusCoordinator coordinator,
        Func<IKeyboardFocusScope?> currentScope,
        bool restoreInitialFocus = true)
    {
        if (window is null)
            throw new ArgumentNullException(nameof(window));
        if (coordinator is null)
            throw new ArgumentNullException(nameof(coordinator));
        if (currentScope is null)
            throw new ArgumentNullException(nameof(currentScope));

        return new ActivationFocusSubscription(
            window,
            coordinator,
            currentScope,
            restoreInitialFocus);
    }

    private sealed class ActivationFocusSubscription : IDisposable
    {
        private readonly Window _window;
        private readonly KeyboardFocusCoordinator _coordinator;
        private readonly Func<IKeyboardFocusScope?> _currentScope;
        private readonly bool _restoreInitialFocus;
        private bool _hasActivated;
        private bool _disposed;

        public ActivationFocusSubscription(
            Window window,
            KeyboardFocusCoordinator coordinator,
            Func<IKeyboardFocusScope?> currentScope,
            bool restoreInitialFocus)
        {
            _window = window;
            _coordinator = coordinator;
            _currentScope = currentScope;
            _restoreInitialFocus = restoreInitialFocus;
            _window.Activated += OnActivated;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _window.Activated -= OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            var initial = !_hasActivated;
            _hasActivated = true;

            if (initial && !_restoreInitialFocus)
                return;

            var reason = initial
                ? KeyboardFocusReason.InitialWindowOpened
                : KeyboardFocusReason.WindowReactivated;

            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_disposed)
                        return;

                    var scope = _currentScope();
                    if (scope is not null)
                        _coordinator.EnsureFocus(scope, reason);
                },
                DispatcherPriority.Input);
        }
    }
}
