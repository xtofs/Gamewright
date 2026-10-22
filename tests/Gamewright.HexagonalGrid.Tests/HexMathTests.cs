namespace Gamewright.HexagonalGrid.Tests;

public class HexMathTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 7)]
    [InlineData(3, 19)]
    [InlineData(8, 169)]
    public void HexagonCountIsTheCenteredHexagonalNumber(int radius, int count) =>
        Assert.Equal(count, HexMath.HexagonCount(radius));

    [Fact]
    public void RingOfIndexMatchesTheRingOfTheHex()
    {
        for (var index = 0; index < HexMath.HexagonCount(9); index++)
            Assert.Equal(Hex.FromIndex(index).Ring, HexMath.RingOfIndex(index));
    }

    [Theory]
    [InlineData(0, 0, 0, 3, true)]
    [InlineData(2, -2, 0, 3, true)]
    [InlineData(3, -3, 0, 3, false)]
    [InlineData(0, 0, 0, 1, true)]
    [InlineData(1, -1, 0, 1, false)]
    public void IsOnBoardComparesTheRingWithTheRadius(int q, int r, int s, int radius, bool expected) =>
        Assert.Equal(expected, HexMath.IsOnBoard(new Hex(q, r, s), radius));

    [Fact]
    public void EnumerateHexesYieldsEveryCellOnceInIndexOrder()
    {
        var all = HexMath.EnumerateHexes(5).ToList();

        Assert.Equal(HexMath.HexagonCount(5), all.Count);
        Assert.Equal(Enumerable.Range(0, all.Count), all.Select(pair => pair.Item2));
        Assert.Equal(all.Count, all.Select(pair => pair.Item1).Distinct().Count());
    }

    [Theory]
    [InlineData(0f, 0f, 0, 0)]
    [InlineData(0.4f, -0.4f, 0, 0)]
    [InlineData(1.2f, -0.1f, 1, 0)]
    [InlineData(-1.1f, 2.2f, -1, 2)]
    public void RoundFindsTheNearestHex(float q, float r, int expectedQ, int expectedR)
    {
        var hex = HexMath.Round(q, r, -q - r);

        Assert.Equal(new Hex(expectedQ, expectedR, -expectedQ - expectedR), hex);
    }

    [Fact]
    public void RoundAlwaysProducesAValidHex()
    {
        for (var i = 0; i < 200; i++)
        {
            var q = (i % 17 - 8) * 0.37f;
            var r = (i % 13 - 6) * 0.41f;
            var hex = HexMath.Round(q, r, -q - r);
            Assert.Equal(0, hex.Q + hex.R + hex.S);
        }
    }

    [Fact]
    public void NeighborSetsReflectTheBoardEdge()
    {
        const int radius = 5;
        var counts = HexMath.EnumerateHexes(radius)
            .Select(pair => (Hex: pair.Item1, Count: Neighbor.All.Count(n => HexMath.CreateNeighborSet(pair.Item1, radius).Has(n))))
            .ToList();

        Assert.Equal(6, counts.Single(c => c.Hex == new Hex(0, 0, 0)).Count);
        Assert.Equal(3, counts.Single(c => c.Hex == new Hex(4, -4, 0)).Count);   // a corner cell
        Assert.Equal(4, counts.Single(c => c.Hex == new Hex(4, -2, -2)).Count);  // an edge cell
        Assert.Equal(6, counts.Count(c => c.Count == 3));
    }
}
