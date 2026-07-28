namespace DevTools.HostLogging.Sharp;

internal static class HostLogArgsParser
{
    #region Diagnostics

    private const string DiagnosticsPrefix = "[DevTools.HostLogging]";
    private const string SwitchRequiresModeMessage = "{0} requires a mode ('console' or 'file <path>'). Logging disabled.";
    private const string FileRequiresPathMessage = "{0} file requires a path. Logging disabled.";
    private const string UnknownModeMessage = "Unknown {0} mode '{1}' — expected 'console' or 'file <path>'. Logging disabled.";

    #endregion

    public static HostLogOptions ParseAndRemove(ref string[] args)
    {
        if (args.Length == 0)
        {
            return new HostLogOptions();
        }

        var remaining = new List<string>(args.Length);
        HostLogOptions? parsed = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (!IsSwitch(args[i]))
            {
                remaining.Add(args[i]);
                continue;
            }

            if (i + 1 >= args.Length)
            {
                Warn(string.Format(SwitchRequiresModeMessage, HostLogOptions.SwitchName));
                continue;
            }

            var mode = args[++i];
            if (mode.Equals(HostLogOptions.ConsoleMode, StringComparison.OrdinalIgnoreCase))
            {
                parsed = new HostLogOptions { Sink = HostLogSink.Console };
                continue;
            }

            if (mode.Equals(HostLogOptions.FileMode, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    Warn(string.Format(FileRequiresPathMessage, HostLogOptions.SwitchName));
                    continue;
                }

                parsed = new HostLogOptions
                {
                    Sink = HostLogSink.File,
                    FilePath = args[++i]
                };
                continue;
            }

            Warn(string.Format(UnknownModeMessage, HostLogOptions.SwitchName, mode));
        }

        args = remaining.ToArray();
        return parsed ?? new HostLogOptions();
    }

    private static bool IsSwitch(string value) =>
        value.Equals(HostLogOptions.SwitchName, StringComparison.OrdinalIgnoreCase);

    private static void Warn(string message) =>
        Console.Error.WriteLine($"{DiagnosticsPrefix} {message}");
}
