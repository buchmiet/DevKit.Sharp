namespace Sample.ViewModels;

public sealed class ColorPanelViewModel(string title, string hexColor)
{
    public string Title { get; } = title;

    public string HexColor { get; } = hexColor;
}
