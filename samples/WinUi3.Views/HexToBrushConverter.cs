using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUi3.Views;

public sealed class HexToBrushConverter : IValueConverter
{
    #region Hex parsing

    private const char HexPrefix = '#';
    private const int RgbHexLength = 6;
    private const string OpaqueAlphaPrefix = "FF";
    private const int AlphaShift = 24;
    private const int RedShift = 16;
    private const int GreenShift = 8;
    private const byte ChannelMask = 0xFF;

    #endregion

    public static HexToBrushConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string hex)
            return new SolidColorBrush(Colors.Transparent);

        hex = hex.TrimStart(HexPrefix);
        if (hex.Length == RgbHexLength)
            hex = OpaqueAlphaPrefix + hex;

        return uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var argb)
            ? new SolidColorBrush(Color.FromArgb(
                (byte)((argb >> AlphaShift) & ChannelMask),
                (byte)((argb >> RedShift) & ChannelMask),
                (byte)((argb >> GreenShift) & ChannelMask),
                (byte)(argb & ChannelMask)))
            : new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
