using System.Text;

namespace DevTools.HostLogging.Sharp;

public sealed class FileStartupSession : IStartupSession
{
    #region Log formats

    private const string TimestampFormat = "HH:mm:ss.ffffff";
    private const string LoggerOpenedFormat = "Host event logger file: {0}";
    private const string LogLineFormat = "[{0}] {1}";
    private const string ProgressBeginFormat = "[progress] begin {0} steps";
    private const string ProgressStepFormat = "[progress] {0}/{1}";

    #endregion

    private readonly StreamWriter _writer;
    private readonly Lock _sync = new();
    private int _closed;
    private int _totalSteps;
    private int _completedSteps;

    public FileStartupSession(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(filePath, append: false, new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        Write(string.Format(LoggerOpenedFormat, filePath));
    }

    public bool IsEnabled => true;

    public void Write(string message)
    {
        if (Volatile.Read(ref _closed) != 0)
        {
            return;
        }

        lock (_sync)
        {
            _writer.WriteLine(string.Format(LogLineFormat, DateTime.Now.ToString(TimestampFormat), message ?? string.Empty));
        }
    }

    public void BeginProgress(int totalSteps)
    {
        if (Volatile.Read(ref _closed) != 0)
        {
            return;
        }

        _totalSteps = totalSteps;
        _completedSteps = 0;
        Write(string.Format(ProgressBeginFormat, totalSteps));
    }

    public void CompleteStep(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Write(message);
        }

        if (Volatile.Read(ref _closed) != 0)
        {
            return;
        }

        _completedSteps++;
        if (_totalSteps > 0)
        {
            Write(string.Format(ProgressStepFormat, _completedSteps, _totalSteps));
        }
    }

    public void Close() => Interlocked.Exchange(ref _closed, 1);

    public void Dispose()
    {
        Close();
        _writer.Dispose();
    }
}
