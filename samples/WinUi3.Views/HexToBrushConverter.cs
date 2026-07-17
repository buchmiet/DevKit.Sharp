using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUi3.Views;

public sealed class HexToBrushConverter : IValueConverter
{
    public static HexToBrushConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string hex)
            return new SolidColorBrush(Colors.Transparent);

        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            hex = "FF" + hex;

        return uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var argb)
            ? new SolidColorBrush(Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF)))
            : new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
