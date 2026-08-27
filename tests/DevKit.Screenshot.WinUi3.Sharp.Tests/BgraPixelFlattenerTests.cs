using Windows.UI;

namespace DevKit.Screenshot.WinUi3.Sharp.Tests;

public sealed class BgraPixelFlattenerTests
{
    [Test]
    public async Task FlattenOntoBackground_LeavesOpaquePixelsUnchanged()
    {
        var source = new byte[] { 10, 20, 30, 255 };
        var background = Color.FromArgb(255, 255, 0, 0);

        var flattened = BgraPixelFlattener.FlattenOntoBackground(source, 1, 1, background);

        using var _ = Assert.Multiple();
        await Assert.That(flattened[0]).IsEqualTo((byte)10);
        await Assert.That(flattened[1]).IsEqualTo((byte)20);
        await Assert.That(flattened[2]).IsEqualTo((byte)30);
        await Assert.That(flattened[3]).IsEqualTo(byte.MaxValue);
    }

    [Test]
    public async Task FlattenOntoBackground_ReplacesFullyTransparentPixelsWithBackground()
    {
        var source = new byte[] { 10, 20, 30, 0 };
        var background = Color.FromArgb(255, 255, 128, 64);

        var flattened = BgraPixelFlattener.FlattenOntoBackground(source, 1, 1, background);

        using var _ = Assert.Multiple();
        await Assert.That(flattened[0]).IsEqualTo(background.B);
        await Assert.That(flattened[1]).IsEqualTo(background.G);
        await Assert.That(flattened[2]).IsEqualTo(background.R);
        await Assert.That(flattened[3]).IsEqualTo(byte.MaxValue);
    }

    [Test]
    public async Task FlattenOntoBackground_CompositesPartialAlphaOntoBackground()
    {
        var source = new byte[] { 0, 0, 0, 128 };
        var background = Color.FromArgb(255, 255, 255, 255);

        var flattened = BgraPixelFlattener.FlattenOntoBackground(source, 1, 1, background);

        using var _ = Assert.Multiple();
        await Assert.That(flattened[0]).IsEqualTo((byte)127);
        await Assert.That(flattened[1]).IsEqualTo((byte)127);
        await Assert.That(flattened[2]).IsEqualTo((byte)127);
        await Assert.That(flattened[3]).IsEqualTo(byte.MaxValue);
    }
}
