namespace Gamewright.HexagonalGrid.Tests;

public class HexTests
{
    private static readonly Hex[] Around =
        [(1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1)];

    [Fact]
    public void SIsDerivedFromQAndR()
    {
        var hex = new Hex(2, -5, 3);

        Assert.Equal(3, hex.S);
        Assert.Equal(0, hex.Q + hex.R + hex.S);
    }

    [Fact]
    public void CoordinatesMustSumToZero() =>
        Assert.Throws<ArgumentException>(() => new Hex(1, 1, 1));

    [Fact]
    public void ConvertsToAndFromTuples()
    {
        Hex hex = (1, -3, 2);
        (int q, int r, int s) = hex;

        Assert.Equal(new Hex(1, -3, 2), hex);
        Assert.Equal((1, -3, 2), (q, r, s));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, -1, 0, 1)]
    [InlineData(2, -1, -1, 2)]
    [InlineData(-3, 3, 0, 3)]
    [InlineData(0, 4, -4, 4)]
    public void RingIsTheLargestAbsoluteCoordinate(int q, int r, int s, int ring) =>
        Assert.Equal(ring, new Hex(q, r, s).Ring);

    [Fact]
    public void IndexGrowsFromTheCenterOutward()
    {
        Assert.Equal(0, new Hex(0, 0, 0).Index);
        Assert.Equal(Enumerable.Range(1, 6), Around.Select(h => h.Index).Order());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void IndexAndHexAreInverseOnEveryCellOfABoard(int radius)
    {
        var count = HexMath.HexagonCount(radius);

        for (var index = 0; index < count; index++)
        {
            var hex = Hex.FromIndex(index);
            Assert.Equal(index, hex.Index);
            Assert.True(HexMath.IsOnBoard(hex, radius));
        }
    }

    [Fact]
    public void NeighboringHexesNeverShareAColorClass()
    {
        foreach (var (hex, _) in HexMath.EnumerateHexes(6))
        {
            foreach (var offset in Around)
            {
                var neighbor = hex + new HexOffset(offset.Q, offset.R, offset.S);
                Assert.NotEqual(hex.ColorClass, neighbor.ColorClass);
            }
        }
    }

    [Fact]
    public void ColorClassIsInRangeAndTheCenterIsClassZero()
    {
        Assert.Equal(0, new Hex(0, 0, 0).ColorClass);
        Assert.All(HexMath.EnumerateHexes(8), pair => Assert.InRange(pair.Item1.ColorClass, 0, 3));
    }
}
