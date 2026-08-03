using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DevKit.Screenshot.WinUi3.Sharp;

internal static class ElementCaptureBackgroundResolver
{
    internal static Color Resolve(FrameworkElement element, Color? explicitBackground)
    {
        if (explicitBackground is Color color && color.A == byte.MaxValue)
            return color;

        if (TryGetOpaqueBackground(element, out var selfBackground))
            return selfBackground;

        foreach (var ancestor in GetAncestors(element))
        {
            if (TryGetOpaqueBackground(ancestor, out var ancestorBackground))
                return ancestorBackground;
        }

        var root = element.XamlRoot?.Content as FrameworkElement;
        if (root is not null && TryGetOpaqueBackground(root, out var rootBackground))
            return rootBackground;

        return Color.FromArgb(255, 255, 255, 255);
    }

    private static IEnumerable<FrameworkElement> GetAncestors(FrameworkElement element)
    {
        for (var parent = element.Parent as FrameworkElement; parent is not null; parent = parent.Parent as FrameworkElement)
            yield return parent;
    }

    private static bool TryGetOpaqueBackground(FrameworkElement element, out Color color)
    {
        var brush = element switch
        {
            Panel panel => panel.Background,
            Border border => border.Background,
            ContentControl contentControl => contentControl.Background,
            Control control => control.Background,
            _ => null,
        };

        if (brush is SolidColorBrush solidColorBrush && solidColorBrush.Color.A == byte.MaxValue)
        {
            color = solidColorBrush.Color;
            return true;
        }

        color = default;
        return false;
    }
}
