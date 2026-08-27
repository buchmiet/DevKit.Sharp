using DevKit.Focus.Sharp;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DevKit.Focus.WinUi3.Sharp;

/// <summary>Creates XamlRoot-aware WinUI 3 keyboard focus scopes.</summary>
public static class WinUi3KeyboardFocusScope
{
    public static WinUi3FocusScopeBuilder For(FrameworkElement root)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        return new WinUi3FocusScopeBuilder(root);
    }
}

/// <summary>Fluent builder for a WinUI 3 visual-tree focus scope.</summary>
public sealed class WinUi3FocusScopeBuilder
{
    private readonly FrameworkElement _root;
    private readonly List<FrameworkElement> _additionalRoots = new();
    private string? _id;
    private Func<FrameworkElement?>? _defaultTarget;
    private Func<bool>? _availableWhen;

    internal WinUi3FocusScopeBuilder(FrameworkElement root)
    {
        _root = root;
    }

    public WinUi3FocusScopeBuilder Named(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A focus scope id is required.", nameof(id));

        _id = id;
        return this;
    }

    public WinUi3FocusScopeBuilder Default(FrameworkElement target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        _defaultTarget = () => target;
        return this;
    }

    public WinUi3FocusScopeBuilder Default(Func<FrameworkElement?> targetResolver)
    {
        _defaultTarget = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        return this;
    }

    public WinUi3FocusScopeBuilder Include(FrameworkElement additionalRoot)
    {
        if (additionalRoot is null)
            throw new ArgumentNullException(nameof(additionalRoot));

        _additionalRoots.Add(additionalRoot);
        return this;
    }

    public WinUi3FocusScopeBuilder AvailableWhen(Func<bool> predicate)
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

        return new WinUi3ElementFocusScope(
            id!,
            _root,
            _defaultTarget ?? (() => _root),
            _additionalRoots.ToArray(),
            _availableWhen);
    }
}

internal sealed class WinUi3ElementFocusScope : IKeyboardFocusScope
{
    private readonly FrameworkElement _root;
    private readonly Func<FrameworkElement?> _defaultTarget;
    private readonly IReadOnlyList<FrameworkElement> _additionalRoots;
    private readonly Func<bool>? _availableWhen;

    public WinUi3ElementFocusScope(
        string id,
        FrameworkElement root,
        Func<FrameworkElement?> defaultTarget,
        IReadOnlyList<FrameworkElement> additionalRoots,
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
        return TryResolveAvailableTarget(out var target) &&
               target!.Focus(FocusState.Programmatic);
    }

    private bool TryResolveAvailableTarget(out FrameworkElement? target)
    {
        target = null;

        if (_availableWhen is not null && !_availableWhen())
            return false;

        if (_root.XamlRoot is null ||
            _root.Visibility != Visibility.Visible ||
            !_root.IsHitTestVisible)
        {
            return false;
        }

        target = _defaultTarget();
        if (target is null ||
            target.XamlRoot is null ||
            target.Visibility != Visibility.Visible ||
            !target.IsHitTestVisible)
        {
            return false;
        }

        if (target is Control control && !control.IsEnabled)
            return false;

        return true;
    }

    private static bool ContainsFocus(FrameworkElement root)
    {
        var xamlRoot = root.XamlRoot;
        if (xamlRoot is null)
            return false;

        var focused = FocusManager.GetFocusedElement(xamlRoot);
        if (focused is not DependencyObject current)
            return false;

        while (current is not null)
        {
            if (ReferenceEquals(current, root))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}

/// <summary>Convenience, scheduling, and host-lifetime helpers for WinUI 3 applications.</summary>
public static class WinUi3KeyboardFocusExtensions
{
    public static IKeyboardFocusScope CreateKeyboardFocusScope(
        this FrameworkElement root,
        string? id = null,
        FrameworkElement? defaultTarget = null,
        Func<bool>? availableWhen = null)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        var builder = WinUi3KeyboardFocusScope.For(root);
        if (!string.IsNullOrWhiteSpace(id))
            builder.Named(id!);
        if (defaultTarget is not null)
            builder.Default(defaultTarget);
        if (availableWhen is not null)
            builder.AvailableWhen(availableWhen);
        return builder.Build();
    }

    /// <summary>Queues one focus request on the supplied WinUI dispatcher.</summary>
    public static bool PostFocus(
        this KeyboardFocusCoordinator coordinator,
        DispatcherQueue dispatcherQueue,
        IKeyboardFocusScope scope,
        KeyboardFocusRequest request,
        DispatcherQueuePriority priority = DispatcherQueuePriority.High)
    {
        if (coordinator is null)
            throw new ArgumentNullException(nameof(coordinator));
        if (dispatcherQueue is null)
            throw new ArgumentNullException(nameof(dispatcherQueue));
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        return dispatcherQueue.TryEnqueue(priority, () => coordinator.Apply(scope, request));
    }

    /// <summary>
    /// Restores focus for the currently selected scope whenever the WinUI window becomes active.
    /// The first active notification is reported as InitialWindowOpened; later ones as
    /// WindowReactivated. Deactivation notifications are ignored.
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

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                return;

            var initial = !_hasActivated;
            _hasActivated = true;

            if (initial && !_restoreInitialFocus)
                return;

            var reason = initial
                ? KeyboardFocusReason.InitialWindowOpened
                : KeyboardFocusReason.WindowReactivated;

            _window.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                () =>
                {
                    if (_disposed)
                        return;

                    var scope = _currentScope();
                    if (scope is not null)
                        _coordinator.EnsureFocus(scope, reason);
                });
        }
    }
}
