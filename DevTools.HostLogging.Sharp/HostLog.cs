namespace DevTools.HostLogging.Sharp;

/// <summary>
/// Static entry point for opening or attaching a startup reporting session.
/// </summary>
public static class HostLog
{
    #region Environment

    /// <summary>
    /// Environment variable set by <c>DevTools.HostLogging.Runner</c> when the app is spawned with a detached console.
    /// </summary>
    public const string RemoteAttachVariable = "DEVTOOLS_HOSTLOG_ATTACH";

    #endregion

    #region Session messages

    private const int ConsoleCloseDelayMilliseconds = 400;
    private const string ConsoleAllocateFailedMessage = "Failed to allocate a startup console window.";
    private const string FilePathRequiredMessage = "File path is required when sink is File.";
    private const string ConsoleOpenedMessage = "Startup console opened.";

    #endregion

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
            throw new InvalidOperationException(ConsoleAllocateFailedMessage);
        }

        var view = new StartupConsoleView();
        var session = new LocalStartupSession(view);

        if (options.CloseWhenComplete)
        {
            view.OnProgressComplete += () =>
            {
                session.Close();
                // Keep the 100% state visible briefly, then free the console —
                // off the caller's thread, which is often the host app's UI thread.
                _ = Task.Run(async () =>
                {
                    await Task.Delay(ConsoleCloseDelayMilliseconds).ConfigureAwait(false);
                    view.Dispose();
                    NativeConsole.Release();
                });
            };
        }

        session.Write(ConsoleOpenedMessage);
        return session;
    }

    private static IStartupSession OpenFile(HostLogOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new ArgumentException(FilePathRequiredMessage, nameof(options));
        }

        return new FileStartupSession(options.FilePath);
    }
}
