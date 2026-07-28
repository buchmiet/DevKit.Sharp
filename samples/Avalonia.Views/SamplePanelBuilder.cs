using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Sample.ViewModels;

namespace Avalonia.Views;

public static class SamplePanelBuilder
{
    #region Layout constants

    private const double PanelMinHeight = 120;
    private const double CornerRadius = 12;
    private const double ContentMargin = 12;
    private const double TitleFontSize = 20;
    private const double Spacing = 6;
    private const int ColumnCount = 3;
    private const string GridColumnDefinitions = "*,*,*";
    private const string GridRowDefinitions = "Auto,Auto";

    #endregion

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
            ColumnDefinitions = new ColumnDefinitions(GridColumnDefinitions),
            RowDefinitions = new RowDefinitions(GridRowDefinitions),
        };

        for (var i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            var border = new Border
            {
                MinHeight = PanelMinHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(CornerRadius),
                Background = new SolidColorBrush(Color.Parse(panel.HexColor)),
                Child = new TextBlock
                {
                    Text = panel.Title,
                    Margin = new Thickness(ContentMargin),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = TitleFontSize,
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

            var actions = new StackPanel { Spacing = Spacing };
            actions.Children.Add(copyButton);
            actions.Children.Add(saveButton);

            var cell = new StackPanel
            {
                Spacing = Spacing,
                Margin = new Thickness(Spacing),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            cell.Children.Add(border);
            cell.Children.Add(actions);

            Grid.SetColumn(cell, i % ColumnCount);
            Grid.SetRow(cell, i / ColumnCount);
            grid.Children.Add(cell);
        }

        panelsHost.Children.Add(grid);
    }
}
