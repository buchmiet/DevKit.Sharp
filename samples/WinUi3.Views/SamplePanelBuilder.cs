using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sample.ViewModels;

namespace WinUi3.Views;

public static class SamplePanelBuilder
{
    #region Layout constants

    private const double PanelMinHeight = 120;
    private const double CornerRadius = 12;
    private const double ContentMargin = 12;
    private const double TitleFontSize = 20;
    private const double Spacing = 6;
    private const int ColumnCount = 3;

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
            ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() },
            RowDefinitions = { new RowDefinition(), new RowDefinition() },
        };

        for (var column = 0; column < ColumnCount; column++)
        {
            grid.ColumnDefinitions[column].Width = new GridLength(1, GridUnitType.Star);
        }

        for (var row = 0; row < 2; row++)
        {
            grid.RowDefinitions[row].Height = GridLength.Auto;
        }

        for (var i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            var border = new Border
            {
                MinHeight = PanelMinHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(CornerRadius),
                Background = HexToBrushConverter.Instance.Convert(panel.HexColor, typeof(Brush), null!, string.Empty) as Brush,
                Child = new TextBlock
                {
                    Text = panel.Title,
                    Margin = new Thickness(ContentMargin),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = TitleFontSize,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
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
