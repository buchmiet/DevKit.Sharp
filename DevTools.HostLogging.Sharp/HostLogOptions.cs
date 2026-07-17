namespace DevTools.HostLogging.Sharp;

public sealed class HostLogOptions
{
    public const string SwitchName = "--devtools-hostlog";

    public HostLogSink Sink { get; init; } = HostLogSink.None;

    public string? FilePath { get; init; }

    public bool CloseWhenComplete { get; init; } = true;
}
