using System.Runtime.InteropServices;
using System.Text;

namespace DevTools.HostLogging.Sharp;

internal static class NativeConsole
{
    #region Win32 interop

    private const string Kernel32Dll = "kernel32.dll";
    private const string User32Dll = "user32.dll";
    private const int ShowWindowNormal = 5;
    private const string ConsoleTitle = "DevTools.HostLogging — startup";

    #endregion

    [DllImport(Kernel32Dll, SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport(Kernel32Dll)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport(Kernel32Dll, CharSet = CharSet.Unicode)]
    private static extern bool SetConsoleTitle(string lpConsoleTitle);

    [DllImport(Kernel32Dll, SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport(User32Dll)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport(User32Dll)]
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
            SetConsoleTitle(ConsoleTitle);
            ShowWindow(consoleWindow, ShowWindowNormal);
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
