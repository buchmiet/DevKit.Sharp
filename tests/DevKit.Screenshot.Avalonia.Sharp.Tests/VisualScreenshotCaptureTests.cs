using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using DevKit.Screenshot.Avalonia.Sharp;
using DevKit.Screenshot.Sharp;

namespace DevKit.Screenshot.Avalonia.Sharp.Tests;

public sealed class VisualScreenshotCaptureTests
{
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.GetOrStartForAssembly(global::System.Reflection.Assembly.GetExecutingAssembly());

    [Test]
    public async Task CaptureAsync_FlattensTransparencyByDefault()
    {
        var alpha = await Session.Dispatch(async () =>
        {
            var grid = new Grid { Width = 40, Height = 40 };
            var window = new Window { Width = 40, Height = 40, Content = grid };
            window.Show();

            using var bitmap = await VisualScreenshotCapture.CaptureAsync(grid);
            return BitmapTestHelpers.ReadChannel(bitmap, 0, 0, 3);
        }, CancellationToken.None);

        await Assert.That((int)alpha).IsEqualTo(255);
    }

    [Test]
    public async Task CaptureAsync_PreservesTransparencyWhenFlattenDisabled()
    {
        var alpha = await Session.Dispatch(async () =>
        {
            var grid = new Grid { Width = 40, Height = 40 };
            var window = new Window { Width = 40, Height = 40, Content = grid };
            window.Show();

            using var bitmap = await VisualScreenshotCapture.CaptureAsync(
                grid,
                new VisualCaptureOptions { FlattenTransparency = false });
            return BitmapTestHelpers.ReadChannel(bitmap, 0, 0, 3);
        }, CancellationToken.None);

        await Assert.That((int)alpha).IsEqualTo(0);
    }

    [Test]
    public async Task CaptureAsync_UsesExplicitBackgroundWhenProvided()
    {
        var red = await Session.Dispatch(async () =>
        {
            var grid = new Grid { Width = 40, Height = 40 };
            var window = new Window { Width = 40, Height = 40, Content = grid };
            window.Show();

            using var bitmap = await VisualScreenshotCapture.CaptureAsync(
                grid,
                new VisualCaptureOptions { Background = Brushes.Red });
            return BitmapTestHelpers.ReadChannel(bitmap, 0, 0, 2);
        }, CancellationToken.None);

        await Assert.That((int)red).IsEqualTo(255);
    }

    [Test]
    public async Task CaptureCoreAsync_WritesPngOfVisibleWindow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"devkit-avalonia-{Guid.NewGuid():N}.png");

        try
        {
            var result = await Session.Dispatch(async () =>
            {
                var window = new Window
                {
                    Width = 80,
                    Height = 60,
                    Background = Brushes.Lime,
                    Content = new Grid { Width = 80, Height = 60 },
                };
                window.Show();

                return await AvaloniaScreenshot.CaptureCoreAsync(
                    window,
                    new ScreenshotOptions { OutputPath = path, Delay = TimeSpan.Zero },
                    CancellationToken.None);
            }, CancellationToken.None);

            using var _ = Assert.Multiple();
            await Assert.That(result.OutputPath).IsEqualTo(Path.GetFullPath(path));
            await Assert.That(result.PixelWidth).IsGreaterThanOrEqualTo(1);
            await Assert.That(result.PixelHeight).IsGreaterThanOrEqualTo(1);
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(new FileInfo(path).Length).IsGreaterThan(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
