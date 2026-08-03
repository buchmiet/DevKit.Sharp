namespace DevKit.Screenshot.WinUi3.Sharp;

using Windows.UI;

internal static class BgraPixelFlattener
{
    internal static byte[] FlattenOntoBackground(byte[] bgraPremultiplied, int width, int height, Color background)
    {
        var flattened = new byte[bgraPremultiplied.Length];
        var bgB = background.B;
        var bgG = background.G;
        var bgR = background.R;

        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var index = pixel * 4;
            var alpha = bgraPremultiplied[index + 3];

            if (alpha == byte.MaxValue)
            {
                flattened[index] = bgraPremultiplied[index];
                flattened[index + 1] = bgraPremultiplied[index + 1];
                flattened[index + 2] = bgraPremultiplied[index + 2];
                flattened[index + 3] = byte.MaxValue;
                continue;
            }

            if (alpha == 0)
            {
                flattened[index] = bgB;
                flattened[index + 1] = bgG;
                flattened[index + 2] = bgR;
                flattened[index + 3] = byte.MaxValue;
                continue;
            }

            var inverseAlpha = byte.MaxValue - alpha;
            flattened[index] = (byte)Math.Min(
                byte.MaxValue,
                bgraPremultiplied[index] + (bgB * inverseAlpha + 127) / byte.MaxValue);
            flattened[index + 1] = (byte)Math.Min(
                byte.MaxValue,
                bgraPremultiplied[index + 1] + (bgG * inverseAlpha + 127) / byte.MaxValue);
            flattened[index + 2] = (byte)Math.Min(
                byte.MaxValue,
                bgraPremultiplied[index + 2] + (bgR * inverseAlpha + 127) / byte.MaxValue);
            flattened[index + 3] = byte.MaxValue;
        }

        return flattened;
    }
}
