namespace DevTools.Screenshot.Sharp;

/// <summary>
/// Parses the <c>--devtools-screenshot</c> CLI switch family and removes its tokens from the
/// argument list, so the host app never sees them. Mirrors <c>HostLog.Open(ref args)</c> from
/// DevTools.HostLogging.Sharp.
/// </summary>
public static class ScreenshotArgs
{
    /// <summary><c>--devtools-screenshot &lt;path&gt;</c> — enables capture and sets the PNG output path.</summary>
    public const string PathSwitch = "--devtools-screenshot";

    /// <summary><c>--devtools-screenshot-exit</c> — close the app after the capture.</summary>
    public const string ExitSwitch = "--devtools-screenshot-exit";

    /// <summary><c>--devtools-screenshot-delay &lt;ms&gt;</c> — settle delay before capture, in milliseconds.</summary>
    public const string DelaySwitch = "--devtools-screenshot-delay";

    /// <summary>
    /// Parses and removes the screenshot switches from <paramref name="args"/>.
    /// Returns <see cref="ScreenshotOptions.Disabled"/>-equivalent options when no path switch is present.
    /// Malformed switches are reported on stderr and ignored.
    /// </summary>
    public static ScreenshotOptions ParseAndRemove(ref string[] args)
    {
        if (args.Length == 0)
        {
            return ScreenshotOptions.Disabled;
        }

        var remaining = new List<string>(args.Length);
        string? outputPath = null;
        var exitAfterCapture = false;
        var delay = ScreenshotOptions.DefaultDelay;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals(PathSwitch, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    Warn($"{PathSwitch} requires a file path. Screenshot disabled.");
                    continue;
                }

                outputPath = args[++i];
                continue;
            }

            if (arg.Equals(ExitSwitch, StringComparison.OrdinalIgnoreCase))
            {
                exitAfterCapture = true;
                continue;
            }

            if (arg.Equals(DelaySwitch, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out var ms) || ms < 0)
                {
                    Warn($"{DelaySwitch} requires a non-negative delay in milliseconds. Using the default.");
                    continue;
                }

                i++;
                delay = TimeSpan.FromMilliseconds(ms);
                continue;
            }

            remaining.Add(arg);
        }

        args = remaining.ToArray();

        return new ScreenshotOptions
        {
            OutputPath = outputPath,
            ExitAfterCapture = exitAfterCapture,
            Delay = delay,
        };
    }

    private static void Warn(string message) =>
        Console.Error.WriteLine($"[DevTools.Screenshot] {message}");
}
