namespace DevTools.HostLogging.Sharp;

public sealed class HostLogOptions
{
    #region CLI tokens

    public const string SwitchName = "--devtools-hostlog";
    public const string ConsoleMode = "console";
    public const string FileMode = "file";

    #endregion

    public HostLogSink Sink { get; init; } = HostLogSink.None;

    public string? FilePath { get; init; }

    public bool CloseWhenComplete { get; init; } = true;
}
