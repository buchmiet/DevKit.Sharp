using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sample.ViewModels;

namespace WinUi3.Views;

public static class SamplePanelBuilder
{
    public static void Populate(Grid panelsGrid, IReadOnlyList<ColorPanelViewModel> panels)
    {
        panelsGrid.Children.Clear();

        for (var i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            var border = new Border
            {
                Margin = new Thickness(6),
                CornerRadius = new CornerRadius(12),
                Background = HexToBrushConverter.Instance.Convert(panel.HexColor, typeof(Brush), null!, string.Empty) as Brush,
                Child = new TextBlock
                {
                    Text = panel.Title,
                    Margin = new Thickness(12),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                },
            };

            Grid.SetColumn(border, i % 3);
            Grid.SetRow(border, i / 3);
            panelsGrid.Children.Add(border);
        }
    }
}
