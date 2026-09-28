namespace Gamewright.Graphics.Tests;

public sealed class RectExtensionsTests
{
    [Theory]
    [InlineData(200, 100)]
    [InlineData(100, 200)]
    public void Fraction_IsTakenOfTheShorterSide(float width, float height)
    {
        Assert.Equal(10, new Rect(5, 5, width, height).Fraction(0.1f), 4);
    }
}
