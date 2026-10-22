using Gamewright;

namespace Gamewright.Tests;

public class SizeTests
{
    [Fact]
    public void EmptyIsZeroByZero() => Assert.Equal(new Size(0, 0), Size.Empty);

    [Fact]
    public void SquareHasEqualSides() => Assert.Equal(new Size(7, 7), Size.Square(7));

    [Fact]
    public void ScalesAndDivides()
    {
        Assert.Equal(new Size(20, 30), new Size(10, 15) * 2);
        Assert.Equal(new Size(5, 7.5f), new Size(10, 15) / 2);
    }

    [Fact]
    public void ConvertsToADisplacement() =>
        Assert.Equal(new Displacement(3, 4), new Size(3, 4).ToDisplacement());
}
