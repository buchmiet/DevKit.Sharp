using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace DevKit.Screenshot.Avalonia.Sharp;

internal static class CaptureBackgroundResolver
{
    internal static IBrush Resolve(Visual visual, IBrush? explicitBackground)
    {
        if (explicitBackground is not null && IsOpaqueBrush(explicitBackground))
            return explicitBackground;

        if (TryGetOpaqueBackground(visual, out var selfBackground))
            return selfBackground!;

        foreach (var ancestor in visual.GetVisualAncestors())
        {
            if (TryGetOpaqueBackground(ancestor, out var ancestorBackground))
                return ancestorBackground!;
        }

        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel?.Background is not null && IsOpaqueBrush(topLevel.Background))
            return topLevel.Background;

        if (Application.Current?.Resources is not null)
        {
            foreach (var key in ThemeBackgroundResourceKeys)
            {
                if (Application.Current.Resources.TryGetResource(key, null, out var resource)
                    && resource is IBrush themeBrush
                    && IsOpaqueBrush(themeBrush))
                    return themeBrush;
            }
        }

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark
            ? new SolidColorBrush(Color.FromRgb(30, 30, 30))
            : Brushes.White;
    }

    private static readonly string[] ThemeBackgroundResourceKeys =
    [
        "ThemeBackgroundBrush",
        "SystemControlPageBackgroundAltHighBrush",
        "SystemControlBackgroundChromeMediumBrush",
        "SystemControlBackgroundAltHighBrush",
    ];

    private static bool TryGetOpaqueBackground(Visual visual, out IBrush? background)
    {
        background = visual switch
        {
            Panel panel => panel.Background,
            Border border => border.Background,
            ContentControl contentControl => contentControl.Background,
            _ => null,
        };

        return background is not null && IsOpaqueBrush(background);
    }

    private static bool IsOpaqueBrush(IBrush brush) =>
        brush is ISolidColorBrush solidColorBrush && solidColorBrush.Color.A == byte.MaxValue;
}
