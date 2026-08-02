using System.Reflection;
using Microsoft.UI.Xaml;

namespace DevKit.Screenshot.WinUi3.Sharp;

internal static class MainWindowResolver
{
    public static Window Resolve()
    {
        var app = Application.Current
            ?? throw new InvalidOperationException("Application.Current is null.");

        var appType = app.GetType();

        var mainWindowProperty = appType.GetProperty("MainWindow", BindingFlags.Public | BindingFlags.Instance);
        if (mainWindowProperty?.GetValue(app) is Window windowFromProperty)
            return windowFromProperty;

        foreach (var fieldName in new[] { "m_window", "_window", "window" })
        {
            var field = appType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(app) is Window windowFromField)
                return windowFromField;
        }

        throw new InvalidOperationException(
            "Could not resolve the main window. Expose it on Application as a public MainWindow property " +
            "or keep the default WinUI template field (m_window).");
    }
}
