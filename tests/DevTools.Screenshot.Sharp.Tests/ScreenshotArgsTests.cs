using DevTools.Screenshot.Sharp;

namespace DevTools.Screenshot.Sharp.Tests;

public class ScreenshotArgsTests
{
    [Test]
    public async Task ParseAndRemove_PathSwitch_SetsOutputPathAndRemovesTokens()
    {
        var args = new[] { "run", "--devtools-screenshot", @"artifacts\shot.png", "--verbose" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.IsEnabled).IsTrue();
        await Assert.That(options.OutputPath).IsEqualTo(@"artifacts\shot.png");
        await Assert.That(options.CopyToClipboard).IsFalse();
        await Assert.That(options.ExitAfterCapture).IsFalse();
        await Assert.That(options.Delay).IsEqualTo(ScreenshotOptions.DefaultDelay);
        await Assert.That(args).IsEquivalentTo(["run", "--verbose"]);
    }

    [Test]
    public async Task ParseAndRemove_ClipboardSwitch_EnablesClipboardCapture()
    {
        var args = new[] { "run", "--devtools-screenshot-clipboard", "--verbose" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.IsEnabled).IsTrue();
        await Assert.That(options.CopyToClipboard).IsTrue();
        await Assert.That(options.OutputPath).IsNull();
        await Assert.That(args).IsEquivalentTo(["run", "--verbose"]);
    }

    [Test]
    public async Task ParseAndRemove_AllSwitches_ParsesEverything()
    {
        var args = new[]
        {
            "left",
            "--devtools-screenshot", "shot.png",
            "--devtools-screenshot-exit",
            "--devtools-screenshot-delay", "300",
            "right",
        };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.OutputPath).IsEqualTo("shot.png");
        await Assert.That(options.CopyToClipboard).IsFalse();
        await Assert.That(options.ExitAfterCapture).IsTrue();
        await Assert.That(options.Delay).IsEqualTo(TimeSpan.FromMilliseconds(300));
        await Assert.That(args).IsEquivalentTo(["left", "right"]);
    }

    [Test]
    public async Task ParseAndRemove_ClipboardWithExitAndDelay_ParsesEverything()
    {
        var args = new[]
        {
            "--devtools-screenshot-clipboard",
            "--devtools-screenshot-exit",
            "--devtools-screenshot-delay", "250",
        };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.CopyToClipboard).IsTrue();
        await Assert.That(options.ExitAfterCapture).IsTrue();
        await Assert.That(options.Delay).IsEqualTo(TimeSpan.FromMilliseconds(250));
        await Assert.That(args).IsEquivalentTo(Array.Empty<string>());
    }

    [Test]
    public async Task ParseAndRemove_PathAndClipboard_PrefersClipboard()
    {
        var args = new[] { "--devtools-screenshot", "shot.png", "--devtools-screenshot-clipboard" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.CopyToClipboard).IsTrue();
        await Assert.That(options.OutputPath).IsNull();
    }

    [Test]
    public async Task ParseAndRemove_NoSwitches_ReturnsDisabledAndKeepsArgs()
    {
        var args = new[] { "import", "--path", @"C:\data" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(args).IsEquivalentTo(["import", "--path", @"C:\data"]);
    }

    [Test]
    public async Task ParseAndRemove_MissingPath_DisablesCapture()
    {
        var args = new[] { "run", "--devtools-screenshot" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(args).IsEquivalentTo(["run"]);
    }

    [Test]
    public async Task ParseAndRemove_InvalidDelay_FallsBackToDefault()
    {
        var args = new[] { "--devtools-screenshot", "shot.png", "--devtools-screenshot-delay", "-5" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.Delay).IsEqualTo(ScreenshotOptions.DefaultDelay);
        await Assert.That(options.OutputPath).IsEqualTo("shot.png");
    }

    [Test]
    public async Task ParseAndRemove_SwitchesAreCaseInsensitive()
    {
        var args = new[] { "--DevTools-Screenshot", "shot.png", "--DEVTOOLS-SCREENSHOT-EXIT" };

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.OutputPath).IsEqualTo("shot.png");
        await Assert.That(options.ExitAfterCapture).IsTrue();
        await Assert.That(args).IsEquivalentTo(Array.Empty<string>());
    }

    [Test]
    public async Task EnsureEnabled_Disabled_Throws()
    {
        var action = () => { ScreenshotOptions.Disabled.EnsureEnabled(); };
        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RequireOutputPath_Disabled_Throws()
    {
        var action = () => { ScreenshotOptions.Disabled.RequireOutputPath(); };
        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RequireOutputPath_ClipboardOnly_Throws()
    {
        var options = new ScreenshotOptions { CopyToClipboard = true };
        var action = () => { options.RequireOutputPath(); };
        await Assert.That(action).Throws<InvalidOperationException>();
    }
}
