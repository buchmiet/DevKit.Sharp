using System.Runtime.InteropServices;
using System.Text;

namespace HostEventLauncher.Sharp;

internal static class NativeConsole
{
    private const int SwShow = 5;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetConsoleTitle(string lpConsoleTitle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static bool HasConsoleWindow() => GetConsoleWindow() != IntPtr.Zero;

    public static bool TryAllocate()
    {
        if (GetConsoleWindow() != IntPtr.Zero)
        {
            return true;
        }

        // Always allocate a dedicated console for GUI (WinExe) hosts.
        // Do not AttachConsole(parent) — that hides boot logs in an invisible parent terminal.
        if (!AllocConsole())
        {
            return false;
        }

        BindStandardHandles();
        var consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            SetConsoleTitle("HostEventLauncher — startup");
            ShowWindow(consoleWindow, SwShow);
            SetForegroundWindow(consoleWindow);
        }

        return true;
    }

    public static void Release()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (GetConsoleWindow() != IntPtr.Zero)
        {
            FreeConsole();
        }
    }

    private static void BindStandardHandles()
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
    }
}
