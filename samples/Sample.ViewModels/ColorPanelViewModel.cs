namespace Sample.ViewModels;

public sealed class ColorPanelViewModel(string title, string hexColor)
{
    public string Title { get; } = title;

    public string HexColor { get; } = hexColor;

    public string CopyToClipboardButtonText => $"Copy {Title} to clipboard";

    public string SaveToFileButtonText => $"Save {Title} to file…";

    public string SuggestedFileName => $"{Title.ToLowerInvariant()}-panel.png";
}
