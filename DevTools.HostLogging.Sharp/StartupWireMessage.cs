using System.Globalization;

namespace DevTools.HostLogging.Sharp;

internal readonly record struct StartupWireMessage(
    string Kind,
    string TimestampUtc,
    string Payload)
{
    #region Wire protocol

    internal const string ProgressKind = "progress";
    internal const string LogKind = "log";
    internal const string ControlKind = "control";
    internal const string StepPayload = "step";
    internal const char FieldSeparator = '\t';
    internal const int FieldCount = 3;
    internal const string TimestampFormat = "O";
    private const string CarriageReturnReplacement = " ";
    private const string NewlineReplacement = " ";
    private const string TabReplacement = "    ";

    #endregion

    public static StartupWireMessage BeginProgress(int totalSteps) =>
        new(ProgressKind, DateTime.UtcNow.ToString(TimestampFormat), Sanitize(totalSteps.ToString()));

    public static StartupWireMessage CompleteStep() =>
        new(ProgressKind, DateTime.UtcNow.ToString(TimestampFormat), Sanitize(StepPayload));

    public static StartupWireMessage CreateLog(StartupEntry entry) =>
        new(LogKind, entry.Timestamp.ToUniversalTime().ToString(TimestampFormat), Sanitize(entry.Text));

    public static StartupWireMessage CreateControl(string command) =>
        new(ControlKind, DateTime.UtcNow.ToString(TimestampFormat), Sanitize(command));

    public string Serialize() => string.Join(FieldSeparator, Kind, TimestampUtc, Payload);

    public static bool TryParse(string line, out StartupWireMessage message)
    {
        var parts = line.Split(FieldSeparator, FieldCount, StringSplitOptions.None);
        if (parts.Length != FieldCount)
        {
            message = default;
            return false;
        }

        if (!DateTime.TryParse(
                parts[1],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var timestampUtc))
        {
            message = default;
            return false;
        }

        message = new StartupWireMessage(parts[0], timestampUtc.ToString(TimestampFormat), parts[2]);
        return true;
    }

    private static string Sanitize(string? text) =>
        (text ?? string.Empty)
            .Replace("\r", CarriageReturnReplacement, StringComparison.Ordinal)
            .Replace("\n", NewlineReplacement, StringComparison.Ordinal)
            .Replace("\t", TabReplacement, StringComparison.Ordinal);
}

internal readonly record struct StartupEntry(DateTime Timestamp, string Text);
