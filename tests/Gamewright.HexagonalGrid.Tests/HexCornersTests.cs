namespace Gamewright.HexagonalGrid.Tests;

public class HexCornersTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CornersLieOnTheCircumcircleAtSixtyDegreeSteps(bool flatSideUp)
    {
        var geo = new BoardGeometry(new Rect(0, 0, 800, 800), 5, flatSideUp);
        var hex = new Hex(1, -2, 1);
        var center = geo.HexCenter(hex);

        var corners = geo.HexCorners(hex);

        for (var i = 0; i < 6; i++)
        {
            var spoke = corners[i] - center;
            Assert.Equal(geo.HexagonCircumRadius, spoke.Length, 2);

            var next = corners[(i + 1) % 6] - center;
            // Neighboring corners are one circumradius apart in a regular hexagon.
            Assert.Equal(geo.HexagonCircumRadius, (corners[(i + 1) % 6] - corners[i]).Length, 2);
            Assert.NotEqual(spoke, next);
        }
    }

    [Fact]
    public void ANeighborSharesTwoCornersWithItsHex()
    {
        var geo = new BoardGeometry(new Rect(0, 0, 800, 800), 5);
        var a = geo.HexCorners(new Hex(0, 0, 0));
        var b = geo.HexCorners(new Hex(1, -1, 0));

        static (int, int) Key(Point p) => ((int)MathF.Round(p.X * 10), (int)MathF.Round(p.Y * 10));
        var shared = Enumerable.Range(0, 6).Select(i => Key(a[i])).Intersect(Enumerable.Range(0, 6).Select(i => Key(b[i])));

        Assert.Equal(2, shared.Count());
    }

    [Fact]
    public void ABoardFitsInsideItsBounds()
    {
        var bounds = new Rect(10, 20, 600, 400);
        var geo = new BoardGeometry(bounds, 8);

        foreach (var hex in geo.GridHexes)
        {
            var corners = geo.HexCorners(hex);
            for (var i = 0; i < 6; i++)
            {
                Assert.InRange(corners[i].X, bounds.X - 0.01f, bounds.Right + 0.01f);
                Assert.InRange(corners[i].Y, bounds.Y - 0.01f, bounds.Bottom + 0.01f);
            }
        }
    }

    [Fact]
    public void EmptyBoundsLayNothingOut()
    {
        var geo = new BoardGeometry(new Rect(0, 0, 0, 0), 5);

        Assert.Equal(0f, geo.HexagonCircumRadius);
        Assert.False(geo.TryGetHex(new Point(0, 0), out _));
    }
}
