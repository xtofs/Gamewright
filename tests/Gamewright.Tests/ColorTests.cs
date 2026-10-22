using Gamewright;

namespace Gamewright.Tests;

public class ColorTests
{
    [Fact]
    public void WithAlphaScalesTheAlphaChannelOnly()
    {
        var yellow = new Color(255, 255, 0);

        var translucent = yellow.WithAlpha(0.3f);

        Assert.Equal(new Color(255, 255, 0, 77), translucent);
    }

    [Fact]
    public void WithAlphaMultipliesAnExistingAlpha() =>
        Assert.Equal(new Color(1, 2, 3, 100), new Color(1, 2, 3, 200).WithAlpha(0.5f));

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(1f, 255)]
    [InlineData(2f, 255)]
    [InlineData(-1f, 0)]
    public void WithAlphaIsClamped(float factor, int expected) =>
        Assert.Equal((byte)expected, new Color(9, 9, 9).WithAlpha(factor).A);

    [Fact]
    public void WithAlphaRejectsNonFiniteFactors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Color(1, 2, 3).WithAlpha(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Color(1, 2, 3).WithAlpha(float.PositiveInfinity));
    }

    [Fact]
    public void RgbaPacksRedFirstInTheLowestByte() =>
        Assert.Equal(0x44332211u, new Color(0x11, 0x22, 0x33, 0x44).Rgba);
}
