using DevKit.Focus.WinUi3.Sharp;

namespace DevKit.Focus.WinUi3.Sharp.Tests;

[NotInParallel]
public sealed class WinUi3FocusTreeTests
{
    [Test]
    public async Task Shown_focusable_control_receives_and_reports_focus()
    {
        var snapshot = WinUiSta.Run(WinUiTreeFocusHost.FocusDefaultOnShownButton);

        using var _ = Assert.Multiple();
        await Assert.That(snapshot.CanReceiveFocus).IsTrue();
        await Assert.That(snapshot.TryFocusDefault).IsTrue();
        await Assert.That(snapshot.ContainsKeyboardFocus).IsTrue();
    }

    [Test]
    public async Task Include_counts_focus_in_an_additional_root()
    {
        var insideInclude = WinUiSta.Run(WinUiTreeFocusHost.IncludeCountsFocusInAdditionalRoot);

        await Assert.That(insideInclude).IsTrue();
    }
}
