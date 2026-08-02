namespace DevKit.Logging.Sharp;

public sealed class HostLogOptions
{
    public const string SwitchName = "--devkit-logging";

    public HostLogSink Sink { get; init; } = HostLogSink.None;

    public string? FilePath { get; init; }

    public bool CloseWhenComplete { get; init; } = true;
}
