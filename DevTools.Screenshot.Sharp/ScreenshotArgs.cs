namespace DevTools.Screenshot.Sharp;

/// <summary>
/// Parses the <c>--devtools-screenshot</c> CLI switch family and removes its tokens from the
/// argument list, so the host app never sees them. Mirrors <c>HostLog.Open(ref args)</c> from
/// DevTools.HostLogging.Sharp.
/// </summary>
public static class ScreenshotArgs
{
    #region CLI switches

    /// <summary><c>--devtools-screenshot &lt;path&gt;</c> — enables capture and sets the PNG output path.</summary>
    public const string PathSwitch = "--devtools-screenshot";

    /// <summary><c>--devtools-screenshot-clipboard</c> — places the capture on the system clipboard.</summary>
    public const string ClipboardSwitch = "--devtools-screenshot-clipboard";

    /// <summary><c>--devtools-screenshot-exit</c> — close the app after the capture.</summary>
    public const string ExitSwitch = "--devtools-screenshot-exit";

    /// <summary><c>--devtools-screenshot-delay &lt;ms&gt;</c> — settle delay before capture, in milliseconds.</summary>
    public const string DelaySwitch = "--devtools-screenshot-delay";

    #endregion

    #region Diagnostics

    /// <summary>Prefix written to stderr for CLI parse warnings and capture failures.</summary>
    public const string DiagnosticsPrefix = "[DevTools.Screenshot]";

    private const string PathSwitchRequiresPathMessage = "{0} requires a file path. Screenshot disabled.";
    private const string DelaySwitchRequiresDelayMessage = "{0} requires a non-negative delay in milliseconds. Using the default.";
    private const string PathAndClipboardConflictMessage = "Both {0} and {1} were specified; using the clipboard.";

    #endregion

    /// <summary>
    /// Parses and removes the screenshot switches from <paramref name="args"/>.
    /// Returns <see cref="ScreenshotOptions.Disabled"/>-equivalent options when no destination switch is present.
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
        var copyToClipboard = false;
        var exitAfterCapture = false;
        var delay = ScreenshotOptions.DefaultDelay;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals(PathSwitch, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    Warn(string.Format(PathSwitchRequiresPathMessage, PathSwitch));
                    continue;
                }

                outputPath = args[++i];
                continue;
            }

            if (arg.Equals(ClipboardSwitch, StringComparison.OrdinalIgnoreCase))
            {
                copyToClipboard = true;
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
                    Warn(string.Format(DelaySwitchRequiresDelayMessage, DelaySwitch));
                    continue;
                }

                i++;
                delay = TimeSpan.FromMilliseconds(ms);
                continue;
            }

            remaining.Add(arg);
        }

        args = remaining.ToArray();

        if (copyToClipboard && outputPath is not null)
        {
            Warn(string.Format(PathAndClipboardConflictMessage, PathSwitch, ClipboardSwitch));
            outputPath = null;
        }

        return new ScreenshotOptions
        {
            OutputPath = outputPath,
            CopyToClipboard = copyToClipboard,
            ExitAfterCapture = exitAfterCapture,
            Delay = delay,
        };
    }

    private static void Warn(string message) =>
        Console.Error.WriteLine($"{DiagnosticsPrefix} {message}");
}
