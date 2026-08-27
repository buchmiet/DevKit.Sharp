using System;
using System.Collections.Generic;

namespace DevKit.Focus.Sharp;

/// <summary>
/// Describes one logical keyboard-focus boundary. A scope may contain one control, a full component,
/// or a composite workspace. The implementation owns all framework-specific focus inspection.
/// </summary>
public interface IKeyboardFocusScope
{
    string Id { get; }

    bool CanReceiveFocus { get; }

    bool ContainsKeyboardFocus { get; }

    bool TryFocusDefault();
}

/// <summary>A UI component that exposes its logical keyboard-focus boundary.</summary>
public interface IKeyboardFocusSurface
{
    IKeyboardFocusScope FocusScope { get; }
}

/// <summary>Controls whether initial/surface focus is owned by the component or its host.</summary>
public enum KeyboardFocusOwnership
{
    Component,
    Host,
}

/// <summary>Determines whether focus already inside the scope is retained.</summary>
public enum KeyboardFocusMode
{
    Ensure,
    ForceDefault,
}

/// <summary>Semantic reason supplied by the host when requesting focus.</summary>
public enum KeyboardFocusReason
{
    InitialWindowOpened,
    SurfaceEntered,
    SurfaceRestored,
    WindowReactivated,
    UserNavigation,
    DialogClosed,
    CommandCompleted,
}

/// <summary>Result of applying one focus request.</summary>
public enum KeyboardFocusOutcome
{
    Focused,
    AlreadyInsideScope,
    ScopeUnavailable,
    Rejected,
}

/// <summary>One host request to validate or move keyboard focus.</summary>
public readonly struct KeyboardFocusRequest
{
    public KeyboardFocusRequest(
        KeyboardFocusReason reason,
        KeyboardFocusMode mode = KeyboardFocusMode.Ensure,
        string? detail = null)
    {
        Reason = reason;
        Mode = mode;
        Detail = detail;
    }

    public KeyboardFocusReason Reason { get; }

    public KeyboardFocusMode Mode { get; }

    public string? Detail { get; }
}

/// <summary>Observable result returned for every coordinator request.</summary>
public readonly struct KeyboardFocusResult
{
    public KeyboardFocusResult(
        string scopeId,
        KeyboardFocusReason reason,
        KeyboardFocusMode mode,
        KeyboardFocusOutcome outcome,
        string? detail)
    {
        ScopeId = scopeId ?? throw new ArgumentNullException(nameof(scopeId));
        Reason = reason;
        Mode = mode;
        Outcome = outcome;
        Detail = detail;
    }

    public string ScopeId { get; }

    public KeyboardFocusReason Reason { get; }

    public KeyboardFocusMode Mode { get; }

    public KeyboardFocusOutcome Outcome { get; }

    public string? Detail { get; }

    public override string ToString()
    {
        var detail = string.IsNullOrWhiteSpace(Detail) ? string.Empty : $" ({Detail})";
        return $"{ScopeId}: {Reason}/{Mode} -> {Outcome}{detail}";
    }
}

/// <summary>
/// Stateless focus policy. The caller remains the source of truth for application mode and chooses
/// the current scope. Invoke on the owning GUI thread unless the framework adapter explicitly
/// schedules the call.
/// </summary>
public sealed class KeyboardFocusCoordinator
{
    private readonly Action<KeyboardFocusResult>? _trace;

    public KeyboardFocusCoordinator(Action<KeyboardFocusResult>? trace = null)
    {
        _trace = trace;
    }

    public KeyboardFocusResult Apply(IKeyboardFocusScope scope, KeyboardFocusRequest request)
    {
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        KeyboardFocusOutcome outcome;
        if (!scope.CanReceiveFocus)
        {
            outcome = KeyboardFocusOutcome.ScopeUnavailable;
        }
        else if (request.Mode == KeyboardFocusMode.Ensure && scope.ContainsKeyboardFocus)
        {
            outcome = KeyboardFocusOutcome.AlreadyInsideScope;
        }
        else
        {
            outcome = scope.TryFocusDefault()
                ? KeyboardFocusOutcome.Focused
                : KeyboardFocusOutcome.Rejected;
        }

        var result = new KeyboardFocusResult(
            scope.Id,
            request.Reason,
            request.Mode,
            outcome,
            request.Detail);

        _trace?.Invoke(result);
        return result;
    }

    public KeyboardFocusResult EnsureFocus(
        IKeyboardFocusScope scope,
        KeyboardFocusReason reason,
        string? detail = null)
    {
        return Apply(scope, new KeyboardFocusRequest(reason, KeyboardFocusMode.Ensure, detail));
    }

    public KeyboardFocusResult FocusDefault(
        IKeyboardFocusScope scope,
        KeyboardFocusReason reason,
        string? detail = null)
    {
        return Apply(scope, new KeyboardFocusRequest(reason, KeyboardFocusMode.ForceDefault, detail));
    }
}

/// <summary>Delegate-backed adapter suitable for host-owned or framework-specific surfaces.</summary>
public sealed class DelegateKeyboardFocusScope : IKeyboardFocusScope
{
    private readonly Func<bool> _canReceiveFocus;
    private readonly Func<bool> _containsKeyboardFocus;
    private readonly Func<bool> _tryFocusDefault;

    public DelegateKeyboardFocusScope(
        string id,
        Func<bool> canReceiveFocus,
        Func<bool> containsKeyboardFocus,
        Func<bool> tryFocusDefault)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A focus scope id is required.", nameof(id));

        Id = id;
        _canReceiveFocus = canReceiveFocus ?? throw new ArgumentNullException(nameof(canReceiveFocus));
        _containsKeyboardFocus = containsKeyboardFocus ?? throw new ArgumentNullException(nameof(containsKeyboardFocus));
        _tryFocusDefault = tryFocusDefault ?? throw new ArgumentNullException(nameof(tryFocusDefault));
    }

    public string Id { get; }

    public bool CanReceiveFocus => _canReceiveFocus();

    public bool ContainsKeyboardFocus => _containsKeyboardFocus();

    public bool TryFocusDefault() => _tryFocusDefault();
}

/// <summary>
/// A logical workspace made from several valid focus scopes and one dynamically selected default.
/// Typical uses include two file panels plus a command line, or an editor plus an owned popup.
/// </summary>
public sealed class CompositeKeyboardFocusScope : IKeyboardFocusScope
{
    private readonly Func<IKeyboardFocusScope?> _defaultScope;
    private readonly Func<IEnumerable<IKeyboardFocusScope>> _members;
    private readonly Func<bool>? _availableWhen;

    public CompositeKeyboardFocusScope(
        string id,
        Func<IKeyboardFocusScope?> defaultScope,
        Func<IEnumerable<IKeyboardFocusScope>> members,
        Func<bool>? availableWhen = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A focus scope id is required.", nameof(id));

        Id = id;
        _defaultScope = defaultScope ?? throw new ArgumentNullException(nameof(defaultScope));
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _availableWhen = availableWhen;
    }

    public string Id { get; }

    public bool CanReceiveFocus
    {
        get
        {
            if (_availableWhen is not null && !_availableWhen())
                return false;

            return _defaultScope()?.CanReceiveFocus == true;
        }
    }

    public bool ContainsKeyboardFocus
    {
        get
        {
            var members = _members();
            if (members is null)
                return false;

            foreach (var member in members)
            {
                if (member is not null && member.ContainsKeyboardFocus)
                    return true;
            }

            return false;
        }
    }

    public bool TryFocusDefault()
    {
        var scope = _defaultScope();
        return scope is not null && scope.CanReceiveFocus && scope.TryFocusDefault();
    }
}
