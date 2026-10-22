namespace Gamewright.HexagonalGrid.Tests;

public class GeometryTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HexUnderItsCenterIsItself(bool flatSideUp)
    {
        var geo = new BoardGeometry(new Rect(10, 20, 400, 300), Radius: 5, flatSideUp);
        var count = 0;
        foreach (var hex in geo.GridHexes)
        {
            Assert.True(geo.TryGetHex(geo.HexCenter(hex), out var found));
            Assert.Equal(hex, found);
            count++;
        }
        Assert.Equal(HexMath.HexagonCount(5), count);
    }

    [Fact]
    public void BoardIsCenteredInBounds()
    {
        var bounds = new Rect(10, 20, 400, 300);
        var geo = new BoardGeometry(bounds, 5);
        Assert.Equal(bounds.Center, geo.HexCenter(new Hex(0, 0, 0)));
    }

    [Fact]
    public void PointOutsideBoardIsNoHex()
    {
        var geo = new BoardGeometry(new Rect(0, 0, 400, 300), 3);
        Assert.False(geo.TryGetHex(new Point(-500, -500), out _));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(2, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    [InlineData(8, true)]
    [InlineData(8, false)]
    public void GridLinesAreDistinctAndComplete(int radius, bool flatSideUp)
    {
        var geo = new BoardGeometry(new Rect(0, 0, 800, 800), radius, flatSideUp);
        var lines = geo.GridLines.ToList();

        var cells = HexMath.HexagonCount(radius);
        var borderEdges = 12 * radius - 6;
        Assert.Equal((6 * cells + borderEdges) / 2, lines.Count);

        // endpoints rounded so that corners shared by neighbors compare equal; order of a line's ends is irrelevant
        static (int, int) Key(Point p) => ((int)MathF.Round(p.X * 100), (int)MathF.Round(p.Y * 100));
        var distinct = lines.Select(l => (Key(l.A), Key(l.B))).Select(l => l.Item1.CompareTo(l.Item2) <= 0 ? l : (l.Item2, l.Item1)).ToHashSet();
        Assert.Equal(lines.Count, distinct.Count);
    }
}

