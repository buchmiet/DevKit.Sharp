using System.Reflection;
using Microsoft.UI.Xaml;

namespace DevTools.Screenshot.WinUi3.Sharp;

internal static class MainWindowResolver
{
    #region Reflection names

    private const string MainWindowPropertyName = "MainWindow";
    private static readonly string[] WindowFieldNames = ["m_window", "_window", "window"];

    private const string ApplicationCurrentNullMessage = "Application.Current is null.";
    private const string MainWindowNotFoundMessage =
        "Could not resolve the main window. Expose it on Application as a public MainWindow property " +
        "or keep the default WinUI template field (m_window).";

    #endregion

    public static Window Resolve()
    {
        var app = Application.Current
            ?? throw new InvalidOperationException(ApplicationCurrentNullMessage);

        var appType = app.GetType();

        var mainWindowProperty = appType.GetProperty(MainWindowPropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (mainWindowProperty?.GetValue(app) is Window windowFromProperty)
            return windowFromProperty;

        foreach (var fieldName in WindowFieldNames)
        {
            var field = appType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(app) is Window windowFromField)
                return windowFromField;
        }

        throw new InvalidOperationException(MainWindowNotFoundMessage);
    }
}
