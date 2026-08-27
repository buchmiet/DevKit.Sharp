using DevKit.Focus.Sharp;
using DevKit.Focus.WinUi3.Sharp;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevKit.Focus.WinUi3.Sharp.Tests;

public class WinUi3FocusContractTests
{
    [Test]
    public async Task For_null_root_throws()
    {
        var action = () => WinUi3KeyboardFocusScope.For(null!);
        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task PostFocus_null_dispatcher_throws_before_queueing()
    {
        var coordinator = new KeyboardFocusCoordinator();
        var scope = new DelegateKeyboardFocusScope("test", () => true, () => false, () => true);
        var request = new KeyboardFocusRequest(KeyboardFocusReason.SurfaceEntered);

        var action = () => coordinator.PostFocus((DispatcherQueue)null!, scope, request);

        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AttachRestoration_null_window_throws()
    {
        var coordinator = new KeyboardFocusCoordinator();
        var action = () => WinUi3KeyboardFocusExtensions.AttachKeyboardFocusRestoration(
            (Window)null!,
            coordinator,
            () => null);

        await Assert.That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task PostFocus_applies_on_a_real_dispatcher_queue()
    {
        var controller = DispatcherQueueController.CreateOnDedicatedThread();
        try
        {
            var applied = new TaskCompletionSource<KeyboardFocusResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var coordinator = new KeyboardFocusCoordinator(result => applied.TrySetResult(result));
            var scope = new DelegateKeyboardFocusScope("test", () => true, () => false, () => true);
            var request = new KeyboardFocusRequest(KeyboardFocusReason.SurfaceEntered);

            var queued = coordinator.PostFocus(controller.DispatcherQueue, scope, request);
            var result = await applied.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var _ = Assert.Multiple();
            await Assert.That(queued).IsTrue();
            await Assert.That(result.Outcome).IsEqualTo(KeyboardFocusOutcome.Focused);
        }
        finally
        {
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            controller.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    controller.ShutdownQueue();
                }
                finally
                {
                    stopped.TrySetResult();
                }
            });
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

[NotInParallel]
public class WinUi3FocusXamlTests
{
    [Test]
    public async Task Builder_uses_element_name_as_default_scope_id()
    {
        var id = WinUiSta.Run(() =>
        {
            var root = new Grid { Name = "editor-root" };
            return WinUi3KeyboardFocusScope.For(root).Build().Id;
        });

        await Assert.That(id).IsEqualTo("editor-root");
    }

    [Test]
    public async Task Builder_uses_type_name_when_element_has_no_name()
    {
        var id = WinUiSta.Run(() =>
        {
            var root = new Grid();
            return WinUi3KeyboardFocusScope.For(root).Build().Id;
        });

        await Assert.That(id).IsEqualTo(nameof(Grid));
    }

    [Test]
    public async Task Named_overrides_element_name()
    {
        var id = WinUiSta.Run(() =>
        {
            var root = new Grid { Name = "editor-root" };
            return WinUi3KeyboardFocusScope.For(root).Named("workspace").Build().Id;
        });

        await Assert.That(id).IsEqualTo("workspace");
    }

    [Test]
    public async Task Detached_element_is_not_available_for_focus()
    {
        var snapshot = WinUiSta.Run(() =>
        {
            var root = new Button { Name = "detached" };
            var scope = root.CreateKeyboardFocusScope("detached");
            return (
                CanReceiveFocus: scope.CanReceiveFocus,
                ContainsKeyboardFocus: scope.ContainsKeyboardFocus,
                Focused: scope.TryFocusDefault());
        });

        using var _ = Assert.Multiple();
        await Assert.That(snapshot.CanReceiveFocus).IsFalse();
        await Assert.That(snapshot.ContainsKeyboardFocus).IsFalse();
        await Assert.That(snapshot.Focused).IsFalse();
    }

    [Test]
    public async Task Availability_predicate_can_disable_an_otherwise_valid_scope()
    {
        var canReceive = WinUiSta.Run(() =>
        {
            var root = new Button();
            return WinUi3KeyboardFocusScope
                .For(root)
                .AvailableWhen(() => false)
                .Build()
                .CanReceiveFocus;
        });

        await Assert.That(canReceive).IsFalse();
    }

    [Test]
    public async Task Include_null_root_throws()
    {
        var threw = WinUiSta.Run(() =>
        {
            var builder = WinUi3KeyboardFocusScope.For(new Grid());
            try
            {
                builder.Include(null!);
                return false;
            }
            catch (ArgumentNullException)
            {
                return true;
            }
        });

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Named_blank_id_throws()
    {
        var threw = WinUiSta.Run(() =>
        {
            var builder = WinUi3KeyboardFocusScope.For(new Grid());
            try
            {
                builder.Named(" ");
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        });

        await Assert.That(threw).IsTrue();
    }
}
