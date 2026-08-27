using DevKit.Screenshot.Sharp;

namespace DevKit.Screenshot.Sharp.Tests;

public class ScreenshotArgsTests
{
    public static IEnumerable<Func<(string[] Input, bool Enabled, string? OutputPath, bool Clipboard, bool Exit, int DelayMs, string[] Remaining)>> ParseCases()
    {
        yield return () => (
            ["run", "--devkit-screenshot", @"artifacts\shot.png", "--verbose"],
            true, @"artifacts\shot.png", false, false, 150, ["run", "--verbose"]);
        yield return () => (
            ["run", "--devkit-screenshot-clipboard", "--verbose"],
            true, null, true, false, 150, ["run", "--verbose"]);
        yield return () => (
            ["left", "--devkit-screenshot", "shot.png", "--devkit-screenshot-exit", "--devkit-screenshot-delay", "300", "right"],
            true, "shot.png", false, true, 300, ["left", "right"]);
        yield return () => (
            ["--devkit-screenshot-clipboard", "--devkit-screenshot-exit", "--devkit-screenshot-delay", "250"],
            true, null, true, true, 250, []);
        yield return () => (
            ["--devkit-screenshot", "shot.png", "--devkit-screenshot-clipboard"],
            true, null, true, false, 150, []);
        yield return () => (
            ["import", "--path", @"C:\data"],
            false, null, false, false, 150, ["import", "--path", @"C:\data"]);
        yield return () => (
            ["run", "--devkit-screenshot"],
            false, null, false, false, 150, ["run"]);
        yield return () => (
            ["--devkit-screenshot", "shot.png", "--devkit-screenshot-delay", "-5"],
            true, "shot.png", false, false, 150, ["-5"]);
        yield return () => (
            ["--DevKit-Screenshot", "shot.png", "--DEVKIT-SCREENSHOT-EXIT"],
            true, "shot.png", false, true, 150, []);
    }

    [Test]
    [MethodDataSource(nameof(ParseCases))]
    public async Task ParseAndRemove_SwitchMatrix(
        string[] input,
        bool enabled,
        string? outputPath,
        bool clipboard,
        bool exit,
        int delayMs,
        string[] remaining)
    {
        var args = input;

        var options = ScreenshotArgs.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(options.IsEnabled).IsEqualTo(enabled);
        await Assert.That(options.OutputPath).IsEqualTo(outputPath);
        await Assert.That(options.CopyToClipboard).IsEqualTo(clipboard);
        await Assert.That(options.ExitAfterCapture).IsEqualTo(exit);
        await Assert.That(options.Delay).IsEqualTo(TimeSpan.FromMilliseconds(delayMs));
        await Assert.That(args).IsEquivalentTo(remaining);
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
