using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Sample.ViewModels;

namespace Avalonia.Views;

public static class SamplePanelBuilder
{
    public static void Populate(
        Panel panelsHost,
        IReadOnlyList<ColorPanelViewModel> panels,
        Func<Border, ColorPanelViewModel, Task> onCopyPanel,
        Func<Border, ColorPanelViewModel, Task> onSavePanel)
    {
        panelsHost.Children.Clear();

        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };

        for (var i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            var border = new Border
            {
                MinHeight = 120,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.Parse(panel.HexColor)),
                Child = new TextBlock
                {
                    Text = panel.Title,
                    Margin = new Thickness(12),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 20,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White,
                },
            };

            var copyButton = new Button
            {
                Content = panel.CopyToClipboardButtonText,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            copyButton.Click += async (_, _) => await onCopyPanel(border, panel);

            var saveButton = new Button
            {
                Content = panel.SaveToFileButtonText,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            saveButton.Click += async (_, _) => await onSavePanel(border, panel);

            var actions = new StackPanel { Spacing = 6 };
            actions.Children.Add(copyButton);
            actions.Children.Add(saveButton);

            var cell = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            cell.Children.Add(border);
            cell.Children.Add(actions);

            Grid.SetColumn(cell, i % 3);
            Grid.SetRow(cell, i / 3);
            grid.Children.Add(cell);
        }

        panelsHost.Children.Add(grid);
    }
}
