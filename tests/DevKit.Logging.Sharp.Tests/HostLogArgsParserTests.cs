using DevKit.Logging.Sharp;

namespace DevKit.Logging.Sharp.Tests;

public class HostLogArgsParserTests
{
    [Test]
    public async Task ParseAndRemove_Console_RemovesSwitchAndLeavesOtherArgs()
    {
        var args = new[] { "import", "--devkit-logging", "console", "--path", "C:\\data" };

        var launch = HostLogArgsParser.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(launch.Sink).IsEqualTo(HostLogSink.Console);
        await Assert.That(args).IsEquivalentTo(["import", "--path", "C:\\data"]);
    }

    [Test]
    public async Task ParseAndRemove_File_RemovesSwitchAndPath()
    {
        var args = new[] { "--devkit-logging", "file", "boot.log", "run" };

        var launch = HostLogArgsParser.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(launch.Sink).IsEqualTo(HostLogSink.File);
        await Assert.That(launch.FilePath).IsEqualTo("boot.log");
        await Assert.That(args).IsEquivalentTo(["run"]);
    }

    [Test]
    public async Task ParseAndRemove_WithoutSwitch_ReturnsNoneAndKeepsArgs()
    {
        var args = new[] { "import", "--path", "C:\\data" };

        var launch = HostLogArgsParser.ParseAndRemove(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(launch.Sink).IsEqualTo(HostLogSink.None);
        await Assert.That(args).IsEquivalentTo(["import", "--path", "C:\\data"]);
    }

    [Test]
    public async Task Open_RemovesLoggerArgsFromRefArray()
    {
        var args = new[] { "left", "--devkit-logging", "file", "boot.log", "right" };

        using var session = HostLog.Open(ref args);

        using var _ = Assert.Multiple();
        await Assert.That(session.IsEnabled).IsTrue();
        await Assert.That(args).IsEquivalentTo(["left", "right"]);
    }
}
