namespace Sample.ViewModels;

public sealed class ColorPanelViewModel(string title, string hexColor)
{
    #region UI copy

    private const string CopyButtonTextFormat = "Copy {0} to clipboard";
    private const string SaveButtonTextFormat = "Save {0} to file…";
    private const string SuggestedFileNameFormat = "{0}-panel.png";

    #endregion

    public string Title { get; } = title;

    public string HexColor { get; } = hexColor;

    public string CopyToClipboardButtonText => string.Format(CopyButtonTextFormat, Title);

    public string SaveToFileButtonText => string.Format(SaveButtonTextFormat, Title);

    public string SuggestedFileName => string.Format(SuggestedFileNameFormat, Title.ToLowerInvariant());
}
