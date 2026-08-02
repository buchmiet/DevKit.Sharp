namespace DevKit.Logging.Sharp;

internal static class HostLogArgsParser
{
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
                Warn($"{HostLogOptions.SwitchName} requires a mode ('console' or 'file <path>'). Logging disabled.");
                continue;
            }

            var mode = args[++i];
            if (mode.Equals("console", StringComparison.OrdinalIgnoreCase))
            {
                parsed = new HostLogOptions { Sink = HostLogSink.Console };
                continue;
            }

            if (mode.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    Warn($"{HostLogOptions.SwitchName} file requires a path. Logging disabled.");
                    continue;
                }

                parsed = new HostLogOptions
                {
                    Sink = HostLogSink.File,
                    FilePath = args[++i]
                };
                continue;
            }

            Warn($"Unknown {HostLogOptions.SwitchName} mode '{mode}' — expected 'console' or 'file <path>'. Logging disabled.");
        }

        args = remaining.ToArray();
        return parsed ?? new HostLogOptions();
    }

    private static bool IsSwitch(string value) =>
        value.Equals(HostLogOptions.SwitchName, StringComparison.OrdinalIgnoreCase);

    private static void Warn(string message) =>
        Console.Error.WriteLine($"[DevKit.Logging] {message}");
}
