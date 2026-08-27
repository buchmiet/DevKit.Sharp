using Avalonia.Controls;
using DevKit.Focus.Avalonia.Sharp;

namespace DevKit.Focus.Avalonia.Sharp.Tests;

public class AvaloniaFocusScopeTests
{
    [Test]
    public async Task Builder_uses_control_name_as_default_scope_id()
    {
        var root = new Border { Name = "editor-root" };

        var scope = AvaloniaKeyboardFocusScope.For(root).Build();

        await Assert.That(scope.Id).IsEqualTo("editor-root");
    }

    [Test]
    public async Task Builder_uses_type_name_when_control_has_no_name()
    {
        var root = new Border();

        var scope = AvaloniaKeyboardFocusScope.For(root).Build();

        await Assert.That(scope.Id).IsEqualTo(nameof(Border));
    }

    [Test]
    public async Task Detached_control_is_not_available_for_focus()
    {
        var root = new Border { Focusable = true };
        var scope = root.CreateKeyboardFocusScope("detached");

        using var _ = Assert.Multiple();
        await Assert.That(scope.CanReceiveFocus).IsFalse();
        await Assert.That(scope.ContainsKeyboardFocus).IsFalse();
        await Assert.That(scope.TryFocusDefault()).IsFalse();
    }

    [Test]
    public async Task Availability_predicate_can_disable_an_otherwise_valid_scope()
    {
        var root = new Border { Focusable = true };
        var scope = AvaloniaKeyboardFocusScope
            .For(root)
            .AvailableWhen(() => false)
            .Build();

        await Assert.That(scope.CanReceiveFocus).IsFalse();
    }

    [Test]
    public async Task For_null_root_throws()
    {
        var action = () => AvaloniaKeyboardFocusScope.For(null!);
        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Include_null_root_throws()
    {
        var builder = AvaloniaKeyboardFocusScope.For(new Border());
        var action = () => builder.Include(null!);
        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Named_blank_id_throws()
    {
        var builder = AvaloniaKeyboardFocusScope.For(new Border());
        var action = () => builder.Named(" ");
        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task Named_overrides_control_name()
    {
        var root = new Border { Name = "editor-root" };

        var scope = AvaloniaKeyboardFocusScope.For(root).Named("workspace").Build();

        await Assert.That(scope.Id).IsEqualTo("workspace");
    }
}
