using DevTools.HostLogging.Sharp.Progress;
using System.Runtime.InteropServices;
using System.Text;

namespace DevTools.HostLogging.Sharp;

internal sealed partial class StartupConsoleView : IDisposable
{
    #region Console UI constants

    private const string Kernel32Dll = "kernel32.dll";
    private const string LogTimestampFormat = "HH.mm.ss.ffffff";
    private const string LogLineFormat = "[{0}]: {1}";
    private const int InitialSpinnerColumn = 1;
    private const int MessageWidthPadding = 2;
    private const int SpinnerColumnPadding = 1;
    private const int ProgressPercentMaximum = 100;
    private const int MinimumProgressBarWidth = 10;
    private const int ProgressLabelSpacing = 1;
    private const int EllipsisLength = 3;
    private const string Ellipsis = "...";
    private const int MinimumWindowWidth = 40;
    private const int FallbackWindowWidth = 120;
    private const int MinimumBufferHeight = 1;
    private const int FallbackBufferHeight = 1_000;
    private const int ProgressRowOffset = 1;
    private const string AsciiSpinnerEnvironmentVariable = "DEVTOOLS_HOSTLOG_ASCII_SPINNER";
    private const string TrueEnvironmentValue = "true";
    private const string EnabledEnvironmentValue = "1";
    private const uint Utf8CodePage = 65001;
    private const int SpinnerIntervalMilliseconds = 80;
    private static readonly string[] UnicodeSpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private static readonly string[] AsciiSpinnerFrames = [".  ", ".. ", "...", " ..", "  .", "   "];

    #endregion

    [LibraryImport(Kernel32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleOutputCP(uint wCodePageID);

    [LibraryImport(Kernel32Dll)]
    private static partial uint GetConsoleOutputCP();

    public event Action? OnProgressComplete;

    private readonly Lock _sync = new();
    private readonly bool _supportsCursorPositioning = !Console.IsOutputRedirected;
    private readonly DotsSpinner _spinner;
    private CancellationTokenSource? _spinnerCts;
    private Task? _spinnerTask;
    private int _spinnerRow = -1;
    private int _spinnerColumn = InitialSpinnerColumn;
    private int _nextRow;
    private bool _cursorHidden;
    private readonly NestedProgressTracker _progressTracker = new();
    private ProgressSnapshot _progress;
    private bool _completionSignaled;

    public StartupConsoleView()
    {
        var unicodeSpinnerEnabled = TryEnableUnicodeSpinner();
        _spinner = new DotsSpinner(unicodeSpinnerEnabled);
        _nextRow = SafeCursorTop();
    }

    public void WriteLine(string text)
    {
        StopSpinner();

        var message = string.Format(LogLineFormat, DateTime.Now.ToString(LogTimestampFormat), text);
        if (!_supportsCursorPositioning)
        {
            Console.WriteLine(Truncate(message, SafeWindowWidth()));
            return;
        }

        lock (_sync)
        {
            var row = EnsureOutputRowLocked();
            var width = SafeWindowWidth();
            var visibleText = Truncate(message, width - MessageWidthPadding);
            _spinnerColumn = Math.Clamp(visibleText.Length + SpinnerColumnPadding, 0, Math.Max(0, width - 1));

            Console.SetCursorPosition(0, row);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', width));
            Console.SetCursorPosition(0, row);
            Console.Write(visibleText);
            Console.ResetColor();
            Console.WriteLine();

            _spinnerRow = row;
            _nextRow = SafeCursorTop();

            if (_progress.RootStepCount > 0)
            {
                DrawProgressBarLocked();
            }
        }
    }

    public void SetProgressTotal(int totalSteps)
    {
        lock (_sync)
        {
            _progress = _progressTracker.BeginProgress(totalSteps);
            if (_progress.ActiveDepth == 1)
            {
                _completionSignaled = false;
            }

            if (totalSteps <= 0 || _spinnerRow < 0 || !_supportsCursorPositioning) return;
            DrawProgressBarLocked();
            RestoreOutputCursorLocked();
        }
    }

    public void AdvanceStep()
    {
        bool completed;

        lock (_sync)
        {
            _progress = _progressTracker.CompleteStep();
            completed = _progress.IsCompleted && !_completionSignaled;

            if (_spinnerRow >= 0 && _supportsCursorPositioning)
            {
                DrawProgressBarLocked();
                RestoreOutputCursorLocked();
            }

            if (completed)
            {
                _completionSignaled = true;
            }
        }

        if (completed)
        {
            OnProgressComplete?.Invoke();
        }
    }

    public void StartSpinner()
    {
        if (!_supportsCursorPositioning)
        {
            return;
        }

        lock (_sync)
        {
            if (_spinnerRow < 0 || _spinnerTask is not null)
            {
                return;
            }

            _spinnerCts = new CancellationTokenSource();
            _spinnerTask = SpinAsync(_spinnerCts.Token);

            if (!_cursorHidden)
            {
                Console.CursorVisible = false;
                _cursorHidden = true;
            }
        }
    }

    public void StopSpinner()
    {
        CancellationTokenSource? spinnerCts;
        Task? spinnerTask;

        lock (_sync)
        {
            spinnerCts = _spinnerCts;
            spinnerTask = _spinnerTask;
            _spinnerCts = null;
            _spinnerTask = null;
        }

        if (spinnerCts is null)
        {
            return;
        }

        spinnerCts.Cancel();

        try
        {
            spinnerTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        lock (_sync)
        {
            ClearSpinnerLocked();

            if (_cursorHidden)
            {
                Console.CursorVisible = true;
                _cursorHidden = false;
            }

            RestoreOutputCursorLocked();
        }

        spinnerCts.Dispose();
    }

    public void Dispose() => StopSpinner();

    private async Task SpinAsync(CancellationToken cancellationToken)
    {
        var frames = _spinner.Frames;
        var frameIndex = 0;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_sync)
                {
                    if (_spinnerRow >= 0)
                    {
                        Console.SetCursorPosition(_spinnerColumn, _spinnerRow);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(frames[frameIndex]);
                        Console.ResetColor();
                        RestoreOutputCursorLocked();
                    }
                }

                frameIndex = (frameIndex + 1) % frames.Count;
                await Task.Delay(_spinner.Interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private int EnsureOutputRowLocked()
    {
        var bufferHeight = SafeBufferHeight();
        var row = Math.Clamp(_nextRow, 0, Math.Max(0, bufferHeight - 1));
        Console.SetCursorPosition(0, row);
        return row;
    }

    private void ClearSpinnerLocked()
    {
        if (_spinnerRow < 0)
        {
            return;
        }

        Console.SetCursorPosition(_spinnerColumn, _spinnerRow);
        Console.Write(' ');
    }

    private void DrawProgressBarLocked()
    {
        if (_progress.RootStepCount <= 0 || _spinnerRow < 0)
        {
            return;
        }

        var progressRow = _spinnerRow + ProgressRowOffset;
        if (progressRow >= SafeBufferHeight())
        {
            return;
        }

        var width = SafeWindowWidth();
        var percent = (int)Math.Round(_progress.Percent, MidpointRounding.AwayFromZero);
        percent = Math.Clamp(percent, 0, ProgressPercentMaximum);
        var label = $"{percent,3}%";

        var barWidth = Math.Max(MinimumProgressBarWidth, width - label.Length - MessageWidthPadding);
        var filledWidth = (int)Math.Round(_progress.Ratio * barWidth, MidpointRounding.AwayFromZero);
        filledWidth = Math.Clamp(filledWidth, 0, barWidth);
        var emptyWidth = barWidth - filledWidth;

        Console.SetCursorPosition(0, progressRow);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write(new string('█', filledWidth));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', emptyWidth));
        Console.ResetColor();
        Console.Write($" {label}");

        var totalWritten = barWidth + ProgressLabelSpacing + label.Length;
        if (totalWritten < width)
        {
            Console.Write(new string(' ', width - totalWritten));
        }
    }

    private void RestoreOutputCursorLocked()
    {
        var bufferHeight = SafeBufferHeight();
        var row = Math.Clamp(_nextRow, 0, Math.Max(0, bufferHeight - 1));
        Console.SetCursorPosition(0, row);
    }

    private static string Truncate(string text, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxWidth)
        {
            return text;
        }

        if (maxWidth <= EllipsisLength)
        {
            return text[..maxWidth];
        }

        return text[..(maxWidth - EllipsisLength)] + Ellipsis;
    }

    private static int SafeWindowWidth()
    {
        try
        {
            return Math.Max(MinimumWindowWidth, Console.WindowWidth);
        }
        catch
        {
            return FallbackWindowWidth;
        }
    }

    private static int SafeBufferHeight()
    {
        try
        {
            return Math.Max(MinimumBufferHeight, Console.BufferHeight);
        }
        catch
        {
            return FallbackBufferHeight;
        }
    }

    private static int SafeCursorTop()
    {
        try
        {
            return Console.CursorTop;
        }
        catch
        {
            return 0;
        }
    }

    private static bool TryEnableUnicodeSpinner()
    {
        var forceAscii = Environment.GetEnvironmentVariable(AsciiSpinnerEnvironmentVariable);
        if (string.Equals(forceAscii, EnabledEnvironmentValue, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(forceAscii, TrueEnvironmentValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                Console.OutputEncoding = new UTF8Encoding(false);
                Console.InputEncoding = new UTF8Encoding(false);
            }
            catch
            {
            }

            return true;
        }

        try
        {
            SetConsoleOutputCP(Utf8CodePage);
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);
            return GetConsoleOutputCP() == Utf8CodePage;
        }
        catch
        {
            return false;
        }
    }

    private sealed class DotsSpinner(bool useUnicode)
    {
        public TimeSpan Interval => TimeSpan.FromMilliseconds(SpinnerIntervalMilliseconds);

        public IReadOnlyList<string> Frames { get; } = useUnicode
            ? UnicodeSpinnerFrames
            : AsciiSpinnerFrames;
    }
}
