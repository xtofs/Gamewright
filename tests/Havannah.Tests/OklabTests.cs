using Gamewright;

namespace Havannah.Tests;

public class OklabTests
{
    private static bool Near(Color a, Color b, int tolerance = 1) =>
        Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

    [Fact]
    public void LightnessOneIsWhiteAndZeroIsBlack()
    {
        Assert.Equal(new Color(255, 255, 255), Oklab.FromOkLab(1f, 0f, 0f));
        Assert.Equal(new Color(0, 0, 0), Oklab.FromOkLab(0f, 0f, 0f));
    }

    [Fact]
    public void TheGrayAxisConvertsToEqualChannels()
    {
        var gray = Oklab.FromOkLab(0.59987f, 0f, 0f);

        Assert.True(Near(gray, new Color(0x80, 0x80, 0x80)), $"{gray}");
    }

    [Fact]
    public void AlphaIsPassedThrough() => Assert.Equal(40, Oklab.FromOkLab(0.5f, 0f, 0f, alpha: 40).A);

    [Fact]
    public void DefaultTetrahedronMatchesItsDocumentedColors()
    {
        var colors = Oklab.GrayTetrahedron();
        Color[] expected = [new(0x85, 0x7B, 0x7A), new(0x75, 0x80, 0x81), new(0x82, 0x84, 0x7B), new(0x83, 0x82, 0x8A)];

        Assert.Equal(4, colors.Length);
        Assert.All(expected, e => Assert.Contains(colors, c => Near(c, e)));
    }

    [Fact]
    public void TetrahedronColorsAreDistinctAndNearGray()
    {
        var colors = Oklab.GrayTetrahedron(centerLightness: 0.39987f, s: 0.013f);

        Assert.Equal(4, colors.Distinct().Count());
        // Lightness 0.4 is a dark gray (about #474747); the tetrahedron only tints it slightly.
        Assert.All(colors, c =>
        {
            int[] channels = [c.R, c.G, c.B];
            Assert.All(channels, channel => Assert.InRange(channel, 0x30, 0x60));
            Assert.InRange(channels.Max() - channels.Min(), 0, 20);
        });
    }

    [Fact]
    public void ARotationOfThreeSixtyDegreesGivesTheSameColors()
    {
        var a = Oklab.GrayTetrahedron(thetaDegrees: 24f);
        var b = Oklab.GrayTetrahedron(thetaDegrees: 384f);

        Assert.All(a.Zip(b), pair => Assert.True(Near(pair.First, pair.Second)));
    }
}
