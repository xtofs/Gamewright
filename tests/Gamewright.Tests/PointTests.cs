using System.Numerics;
using Gamewright;

namespace Gamewright.Tests;

public class PointTests
{
    [Fact]
    public void DifferenceOfPointsIsADisplacement() =>
        Assert.Equal(new Displacement(2, -3), new Point(5, 1) - new Point(3, 4));

    [Fact]
    public void PointPlusAndMinusDisplacement()
    {
        Assert.Equal(new Point(4, 6), new Point(1, 2) + new Displacement(3, 4));
        Assert.Equal(new Point(-2, -2), new Point(1, 2) - new Displacement(3, 4));
    }

    [Fact]
    public void LerpInterpolatesBetweenPoints()
    {
        Assert.Equal(new Point(5, 10), Point.Lerp(new Point(0, 0), new Point(10, 20), 0.5f));
        Assert.Equal(new Point(10, 20), Point.Lerp(new Point(0, 0), new Point(10, 20), 1f));
    }

    [Fact]
    public void ClampMovesAPointIntoARectangleEdgesIncluded()
    {
        var rect = new Rect(10, 20, 100, 50);

        Assert.Equal(new Point(10, 20), new Point(0, 0).Clamp(rect));
        Assert.Equal(new Point(110, 70), new Point(500, 500).Clamp(rect));
        Assert.Equal(new Point(50, 40), new Point(50, 40).Clamp(rect));
    }

    [Fact]
    public void ConvertsToAndFromVector2()
    {
        Assert.Equal(new Vector2(1, 2), (Vector2)new Point(1, 2));
        Assert.Equal(new Point(3, 4), (Point)new Vector2(3, 4));
    }

    [Fact]
    public void ZeroIsTheOrigin() => Assert.Equal(new Point(0, 0), Point.Zero);
}
