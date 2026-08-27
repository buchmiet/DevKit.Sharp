using DevKit.Focus.Sharp;

namespace DevKit.Focus.Sharp.Tests;

public class KeyboardFocusCoordinatorTests
{
    [Test]
    public async Task Ensure_retains_focus_already_inside_scope()
    {
        var focusCalls = 0;
        var scope = Scope(available: true, contains: true, focus: () =>
        {
            focusCalls++;
            return true;
        });

        var result = new KeyboardFocusCoordinator().EnsureFocus(
            scope,
            KeyboardFocusReason.WindowReactivated);

        using var _ = Assert.Multiple();
        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.AlreadyInsideScope);
        await Assert.That(focusCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Ensure_focuses_default_when_scope_is_available_and_empty()
    {
        var scope = Scope(available: true, contains: false, focus: () => true);

        var result = new KeyboardFocusCoordinator().EnsureFocus(
            scope,
            KeyboardFocusReason.SurfaceEntered,
            "Panels -> Terminal");

        using var _ = Assert.Multiple();
        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.Focused);
        await Assert.That(result.Detail).IsEqualTo("Panels -> Terminal");
    }

    [Test]
    public async Task Unavailable_scope_is_not_queried_or_focused()
    {
        var containsCalls = 0;
        var focusCalls = 0;
        var scope = new DelegateKeyboardFocusScope(
            "hidden",
            () => false,
            () =>
            {
                containsCalls++;
                return false;
            },
            () =>
            {
                focusCalls++;
                return true;
            });

        var result = new KeyboardFocusCoordinator().EnsureFocus(
            scope,
            KeyboardFocusReason.SurfaceRestored);

        using var _ = Assert.Multiple();
        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.ScopeUnavailable);
        await Assert.That(containsCalls).IsEqualTo(0);
        await Assert.That(focusCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ForceDefault_moves_focus_even_when_scope_already_contains_it()
    {
        var focusCalls = 0;
        var scope = Scope(available: true, contains: true, focus: () =>
        {
            focusCalls++;
            return true;
        });

        var result = new KeyboardFocusCoordinator().FocusDefault(
            scope,
            KeyboardFocusReason.UserNavigation);

        using var _ = Assert.Multiple();
        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.Focused);
        await Assert.That(focusCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Rejected_focus_is_reported_and_traced()
    {
        KeyboardFocusResult? traced = null;
        var coordinator = new KeyboardFocusCoordinator(result => traced = result);
        var scope = Scope(available: true, contains: false, focus: () => false);

        var result = coordinator.EnsureFocus(scope, KeyboardFocusReason.DialogClosed);

        using var _ = Assert.Multiple();
        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.Rejected);
        await Assert.That(traced.HasValue).IsTrue();
        await Assert.That(traced!.Value.ScopeId).IsEqualTo("test");
    }

    [Test]
    public async Task Composite_accepts_focus_in_any_member_and_uses_dynamic_default()
    {
        var leftFocus = 0;
        var rightFocus = 0;
        var useRight = false;
        var left = Scope("left", true, false, () =>
        {
            leftFocus++;
            return true;
        });
        var right = Scope("right", true, true, () =>
        {
            rightFocus++;
            return true;
        });
        var composite = new CompositeKeyboardFocusScope(
            "panels",
            () => useRight ? right : left,
            () => new[] { left, right });

        var coordinator = new KeyboardFocusCoordinator();
        var ensured = coordinator.EnsureFocus(composite, KeyboardFocusReason.WindowReactivated);
        useRight = true;
        var forced = coordinator.FocusDefault(composite, KeyboardFocusReason.UserNavigation);

        using var _ = Assert.Multiple();
        await Assert.That(ensured.Outcome).IsEqualTo(KeyboardFocusOutcome.AlreadyInsideScope);
        await Assert.That(leftFocus).IsEqualTo(0);
        await Assert.That(forced.Outcome).IsEqualTo(KeyboardFocusOutcome.Focused);
        await Assert.That(rightFocus).IsEqualTo(1);
    }

    [Test]
    public async Task Delegate_empty_id_throws()
    {
        var action = () => new DelegateKeyboardFocusScope(" ", () => true, () => false, () => true);
        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task Apply_null_scope_throws()
    {
        var action = () => new KeyboardFocusCoordinator().Apply(
            null!,
            new KeyboardFocusRequest(KeyboardFocusReason.UserNavigation));
        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Composite_unavailable_when_predicate_is_false()
    {
        var inner = Scope(available: true, contains: false, focus: () => true);
        var composite = new CompositeKeyboardFocusScope(
            "panels",
            () => inner,
            () => new[] { inner },
            () => false);

        var result = new KeyboardFocusCoordinator().EnsureFocus(
            composite,
            KeyboardFocusReason.SurfaceEntered);

        await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.ScopeUnavailable);
    }

    [Test]
    public async Task Composite_members_null_does_not_contain_focus()
    {
        var inner = Scope(available: true, contains: true, focus: () => true);
        var composite = new CompositeKeyboardFocusScope(
            "panels",
            () => inner,
            () => null!);

        await Assert.That(composite.ContainsKeyboardFocus).IsFalse();
    }

    private static DelegateKeyboardFocusScope Scope(
        bool available,
        bool contains,
        Func<bool> focus) => Scope("test", available, contains, focus);

    private static DelegateKeyboardFocusScope Scope(
        string id,
        bool available,
        bool contains,
        Func<bool> focus) =>
        new(id, () => available, () => contains, focus);
}
