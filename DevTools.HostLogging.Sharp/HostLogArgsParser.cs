namespace DevTools.HostLogging.Sharp;

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
                if (i + 1 >= args.Length)
                {
                    continue;
                }

                var filePath = args[++i];
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                parsed = new HostLogOptions
                {
                    Sink = HostLogSink.File,
                    FilePath = filePath
                };
            }
        }

        args = remaining.ToArray();
        return parsed ?? new HostLogOptions();
    }

    private static bool IsSwitch(string value) =>
        value.Equals(HostLogOptions.SwitchName, StringComparison.OrdinalIgnoreCase);
}
