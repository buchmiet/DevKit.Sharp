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
    public async Task Open_FileSink_WritesTimestampedLinesAndStripsArgs()
    {
        var path = Path.Combine(Path.GetTempPath(), $"devkit-log-{Guid.NewGuid():N}.log");
        var args = new[] { "left", "--devkit-logging", "file", path, "right" };

        try
        {
            using (var session = HostLog.Open(ref args))
            {
                session.Write("hello from test");
                session.BeginProgress(2);
                session.CompleteStep("step-one");
                session.CompleteStep();
            }

            var text = await File.ReadAllTextAsync(path);

            using var _ = Assert.Multiple();
            await Assert.That(args).IsEquivalentTo(["left", "right"]);
            await Assert.That(text).Contains("Host event logger file:");
            await Assert.That(text).Contains("hello from test");
            await Assert.That(text).Contains("[progress] begin 2 steps");
            await Assert.That(text).Contains("step-one");
            await Assert.That(text).Contains("[progress] 2/2");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Open_FileSinkWithoutPath_Throws()
    {
        var action = () => HostLog.Open(new HostLogOptions { Sink = HostLogSink.File });
        await Assert.That(action).Throws<ArgumentException>();
    }
}
