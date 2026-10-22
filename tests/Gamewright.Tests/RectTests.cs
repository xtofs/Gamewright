using Gamewright;

namespace Gamewright.Tests;

public class RectTests
{
    [Fact]
    public void ExposesSizeCornersAndCenter()
    {
        var rect = new Rect(10, 20, 100, 50);

        Assert.Equal(new Size(100, 50), rect.Size);
        Assert.Equal(new Point(10, 20), rect.TopLeft);
        Assert.Equal(new Point(60, 45), rect.Center);
        Assert.Equal(110f, rect.Right);
        Assert.Equal(70f, rect.Bottom);
    }

    [Fact]
    public void BuildsFromAPointAndASize() =>
        Assert.Equal(new Rect(1, 2, 3, 4), new Rect(new Point(1, 2), new Size(3, 4)));

    [Fact]
    public void FromCenterCentersTheRectangle()
    {
        var rect = Rect.FromCenter(new Point(50, 40), new Size(20, 10));

        Assert.Equal(new Rect(40, 35, 20, 10), rect);
        Assert.Equal(new Point(50, 40), rect.Center);
    }

    [Fact]
    public void ContainsIncludesTheLeftAndTopEdgesButNotTheRightAndBottom()
    {
        var rect = new Rect(0, 0, 10, 10);

        Assert.True(rect.Contains(new Point(0, 0)));
        Assert.True(rect.Contains(new Point(9.99f, 9.99f)));
        Assert.False(rect.Contains(new Point(10, 5)));
        Assert.False(rect.Contains(new Point(5, 10)));
        Assert.False(rect.Contains(new Point(-1, 5)));
    }

    [Fact]
    public void InflateGrowsAndDeflateShrinksOnEverySide()
    {
        var rect = new Rect(10, 10, 100, 80);

        Assert.Equal(new Rect(5, 0, 110, 100), rect.Inflate(5, 10));
        Assert.Equal(new Rect(25, 25, 70, 50), rect.Deflate(15));
    }

    [Fact]
    public void ConvertsToAndFromTuples()
    {
        Rect rect = (1f, 2f, 3f, 4f);
        (float x, float y, float w, float h) = rect;

        Assert.Equal(new Rect(1, 2, 3, 4), rect);
        Assert.Equal((1f, 2f, 3f, 4f), (x, y, w, h));
    }
}
