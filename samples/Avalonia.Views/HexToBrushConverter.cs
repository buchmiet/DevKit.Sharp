using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Avalonia.Views;

public sealed class HexToBrushConverter : IValueConverter
{
    public static HexToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string hex
            ? new SolidColorBrush(Color.Parse(hex))
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
