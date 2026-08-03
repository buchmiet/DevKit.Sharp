using Avalonia;
using Avalonia.Media.Imaging;

namespace DevKit.Screenshot.Avalonia.Sharp.Tests;

internal static class BitmapTestHelpers
{
    public static byte ReadChannel(Bitmap bitmap, int x, int y, int channelOffset)
    {
        var size = bitmap.PixelSize;
        var stride = size.Width * 4;
        var bytes = new byte[stride * size.Height];
        var rect = new PixelRect(0, 0, size.Width, size.Height);

        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                bitmap.CopyPixels(rect, (IntPtr)ptr, bytes.Length, stride);
            }
        }

        return bytes[y * stride + x * 4 + channelOffset];
    }
}
