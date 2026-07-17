namespace DevTools.HostLogging.Sharp;

/// <summary>
/// Static entry point for opening or attaching a startup reporting session.
/// </summary>
public static class HostLog
{
    /// <summary>
    /// Environment variable set by <c>DevTools.HostLogging.Runner</c> when the app is spawned with a detached console.
    /// </summary>
    public const string RemoteAttachVariable = "DEVTOOLS_HOSTLOG_ATTACH";

    /// <summary>
    /// Parses <c>--devtools-hostlog console|file &lt;path&gt;</c>, removes those tokens from <paramref name="args"/>,
    /// and opens the configured startup session.
    /// </summary>
    public static IStartupSession Open(ref string[] args)
    {
        var launch = HostLogArgsParser.ParseAndRemove(ref args);
        return Open(launch);
    }

    public static IStartupSession Open(HostLogOptions options) =>
        options.Sink switch
        {
            HostLogSink.Console => OpenConsole(options),
            HostLogSink.File => OpenFile(options),
            _ => NullStartupSession.Instance
        };

    /// <summary>
    /// Attaches to a startup console owned by another process (typically <c>DevTools.HostLogging.Runner</c>).
    /// </summary>
    public static IStartupSession Attach(string? attachName = null) =>
        new RemoteStartupSession(attachName);

    private static IStartupSession OpenConsole(HostLogOptions options)
    {
        if (!ConsoleHost.EnsureAttached())
        {
            throw new InvalidOperationException("Failed to allocate a startup console window.");
        }

        var view = new StartupConsoleView();
        var session = new LocalStartupSession(view);

        if (options.CloseWhenComplete)
        {
            view.OnProgressComplete += () =>
            {
                session.Close();
                view.Dispose();
                Thread.Sleep(400);
                NativeConsole.Release();
            };
        }

        session.Write("Startup console opened.");
        return session;
    }

    private static IStartupSession OpenFile(HostLogOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new ArgumentException("File path is required when sink is File.", nameof(options));
        }

        return new FileStartupSession(options.FilePath);
    }
}
