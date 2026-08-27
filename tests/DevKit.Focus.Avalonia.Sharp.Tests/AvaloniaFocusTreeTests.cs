using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using DevKit.Focus.Avalonia.Sharp;
using DevKit.Focus.Sharp;

namespace DevKit.Focus.Avalonia.Sharp.Tests;

public sealed class AvaloniaFocusTreeTests
{
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(AvaloniaFocusTreeTests).Assembly);

    [Test]
    public async Task Shown_focusable_control_receives_and_reports_focus()
    {
        var snapshot = await Session.Dispatch(
            () =>
            {
                var button = new Button { Focusable = true, Width = 40, Height = 40 };
                var window = new Window { Width = 80, Height = 80, Content = button };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var scope = button.CreateKeyboardFocusScope("btn");
                var focused = scope.TryFocusDefault();
                Dispatcher.UIThread.RunJobs();

                return (
                    scope.CanReceiveFocus,
                    focused,
                    scope.ContainsKeyboardFocus);
            },
            CancellationToken.None);

        using var _ = Assert.Multiple();
        await Assert.That(snapshot.CanReceiveFocus).IsTrue();
        await Assert.That(snapshot.focused).IsTrue();
        await Assert.That(snapshot.ContainsKeyboardFocus).IsTrue();
    }

    [Test]
    public async Task Include_counts_focus_in_an_additional_root()
    {
        var insideInclude = await Session.Dispatch(
            () =>
            {
                var left = new Button { Name = "left", Focusable = true };
                var right = new Button { Name = "right", Focusable = true };
                var panel = new StackPanel();
                panel.Children.Add(left);
                panel.Children.Add(right);
                var window = new Window { Width = 200, Height = 80, Content = panel };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                right.Focus();
                Dispatcher.UIThread.RunJobs();

                var scope = AvaloniaKeyboardFocusScope
                    .For(left)
                    .Named("editor")
                    .Include(right)
                    .Build();

                return scope.ContainsKeyboardFocus;
            },
            CancellationToken.None);

        await Assert.That(insideInclude).IsTrue();
    }

    [Test]
    public async Task Default_target_is_focused_instead_of_the_root()
    {
        var focusedName = await Session.Dispatch(
            () =>
            {
                var root = new StackPanel { Name = "root" };
                var target = new Button { Name = "target", Focusable = true };
                root.Children.Add(target);
                var window = new Window { Width = 120, Height = 80, Content = root };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var scope = AvaloniaKeyboardFocusScope
                    .For(root)
                    .Named("workspace")
                    .Default(target)
                    .Build();

                scope.TryFocusDefault();
                Dispatcher.UIThread.RunJobs();

                return window.FocusManager?.GetFocusedElement() is Control focused
                    ? focused.Name
                    : null;
            },
            CancellationToken.None);

        await Assert.That(focusedName).IsEqualTo("target");
    }

    [Test]
    public async Task PostFocus_applies_on_the_UI_thread()
    {
        var outcome = await Session.Dispatch(
            () =>
            {
                var button = new Button { Focusable = true };
                var window = new Window { Width = 80, Height = 80, Content = button };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                KeyboardFocusResult? traced = null;
                var coordinator = new KeyboardFocusCoordinator(result => traced = result);
                var scope = button.CreateKeyboardFocusScope("btn");
                coordinator.PostFocus(
                    scope,
                    new KeyboardFocusRequest(KeyboardFocusReason.SurfaceEntered));
                Dispatcher.UIThread.RunJobs();

                return traced?.Outcome;
            },
            CancellationToken.None);

        await Assert.That(outcome).IsEqualTo(KeyboardFocusOutcome.Focused);
    }

    [Test]
    public async Task AttachRestoration_focuses_on_first_activation()
    {
        var snapshot = await Session.Dispatch(
            () =>
            {
                var button = new Button { Focusable = true };
                var window = new Window { Width = 80, Height = 80, Content = button };
                var coordinator = new KeyboardFocusCoordinator();
                var scope = button.CreateKeyboardFocusScope("btn");

                using (window.AttachKeyboardFocusRestoration(coordinator, () => scope))
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();
                    return (scope.CanReceiveFocus, scope.ContainsKeyboardFocus);
                }
            },
            CancellationToken.None);

        using var _ = Assert.Multiple();
        await Assert.That(snapshot.CanReceiveFocus).IsTrue();
        await Assert.That(snapshot.ContainsKeyboardFocus).IsTrue();
    }
}
