namespace Gamewright.HexBoard.Tests;

public class HexDirectionTests
{
    public static TheoryData<HexDirection> Directions() => new(Enum.GetValues<HexDirection>());

    [Theory]
    [MemberData(nameof(Directions))]
    public void Offset_IsAdjacentToOrigin(HexDirection direction)
    {
        var offset = CubeCoord.Offset(direction);

        Assert.Equal(1, offset.Ring());
    }

    [Theory]
    [MemberData(nameof(Directions))]
    public void Offset_OfOpposite_CancelsOut(HexDirection direction)
    {
        var sum = CubeCoord.Offset(direction) + CubeCoord.Offset(direction.Opposite());

        Assert.Equal(new CubeCoord(0, 0, 0), sum);
    }

    [Fact]
    public void Offsets_AreDistinct()
    {
        var offsets = Enum.GetValues<HexDirection>().Select(CubeCoord.Offset).Distinct();

        Assert.Equal(6, offsets.Count());
    }

    [Theory]
    [MemberData(nameof(Directions))]
    public void RadiusTimesOffset_IsTheCornerWithTheSameDirection(HexDirection direction)
    {
        const int Radius = 4;
        var cube = Radius * CubeCoord.Offset(direction);

        Assert.True(cube.IsCorner(Radius, out var corner));
        Assert.Equal((CornerDirection)direction, corner);
    }
}

public class CubeCoordTests
{
    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(2, -1, -1, true)]
    [InlineData(3, -3, 0, true)]
    [InlineData(0, 3, -3, true)]
    [InlineData(4, -3, -1, false)]
    [InlineData(-2, -2, 4, false)]
    public void IsWithin_Radius3(int q, int r, int s, bool expected)
    {
        Assert.Equal(expected, new CubeCoord(q, r, s).IsWithin(3));
    }
}

public class HexGridTests
{
    private const int Radius = 5;

    private readonly HexGrid<Stone> _grid = new(Radius);

    [Fact]
    public void Neighbors_Count_DependsOnPosition()
    {
        foreach (var (cube, index) in CubeMath.EnumerateGridCoords(Radius))
        {
            var expected = cube.IsCorner(Radius, out _) ? 3
                : cube.Ring() == Radius ? 4
                : 6;

            Assert.Equal(expected, CountValid(_grid.Neighbors(index)));
        }
    }

    [Fact]
    public void Neighbors_TotalLinks_MatchesBoundaryDeficit()
    {
        // every cell has 6 neighbor slots; each of the 6 corners misses 3 and
        // each of the 6*(R-1) other boundary cells misses 2, i.e. 6*(2R+1) in total
        var expected = 6 * _grid.Count - 6 * (2 * Radius + 1);

        var actual = Enumerable.Range(0, _grid.Count).Sum(i => CountValid(_grid.Neighbors(i)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Neighbors_AreSymmetric()
    {
        for (var a = 0; a < _grid.Count; a++)
        {
            foreach (var direction in Enum.GetValues<HexDirection>())
            {
                var b = _grid.Neighbors(a)[(int)direction];
                if (b >= 0)
                {
                    Assert.Equal(a, _grid.Neighbors(b)[(int)direction.Opposite()]);
                }
            }
        }
    }

    [Fact]
    public void Neighbors_MatchCubeNeighbor()
    {
        foreach (var (cube, index) in CubeMath.EnumerateGridCoords(Radius))
        {
            foreach (var direction in Enum.GetValues<HexDirection>())
            {
                var neighbor = cube.Neighbor(direction);
                var expected = neighbor.IsWithin(Radius) ? neighbor.GetIndex() : -1;

                Assert.Equal(expected, _grid.Neighbors(index)[(int)direction]);
            }
        }
    }

    [Fact]
    public void Contains_MatchesTryGetContent()
    {
        var inside = new CubeCoord(Radius, -Radius, 0);
        var outside = new CubeCoord(Radius + 1, -Radius, -1);

        Assert.True(_grid.Contains(inside));
        Assert.True(_grid.TryGetContent(inside, out _));
        Assert.False(_grid.Contains(outside));
        Assert.False(_grid.TryGetContent(outside, out _));
    }

    [Fact]
    public void RegionTracker_ChainAlongNorthEdge_TouchesOnlyNorthEdge()
    {
        var tracker = new HexRegionTracker<char>(_grid.Count);

        // the north edge is s == Radius, its corners are at q == -Radius and q == 0
        for (var q = -Radius + 1; q < 0; q++)
        {
            Place(tracker, new CubeCoord(q, -q - Radius, Radius), 'X');
        }

        var index = new CubeCoord(-1, 1 - Radius, Radius).GetIndex();
        Assert.Equal(1 << (int)EdgeDirection.North, tracker.EdgeMask(index));
        Assert.Equal(0, tracker.CornerMask(index));
    }

    [Fact]
    public void RegionTracker_DoesNotMergeDifferentGroups()
    {
        var tracker = new HexRegionTracker<char>(_grid.Count);
        var a = new CubeCoord(-1, 1 - Radius, Radius);
        var b = a.Neighbor(HexDirection.East);

        Place(tracker, a, 'X');
        Place(tracker, b, 'O');

        Assert.Equal(1 << (int)EdgeDirection.North, tracker.EdgeMask(a.GetIndex()));
        Assert.Equal(0, tracker.CornerMask(a.GetIndex()));
        Assert.Equal(1 << (int)CornerDirection.NorthEast, tracker.CornerMask(b.GetIndex()));
        Assert.Equal(0, tracker.EdgeMask(b.GetIndex()));
    }

    private void Place(HexRegionTracker<char> tracker, CubeCoord cube, char group)
    {
        var index = cube.GetIndex();
        tracker.Register(index, group, _grid.EdgeMask(index), _grid.CornerMask(index), _grid.Neighbors(index));
    }

    private static int CountValid(ReadOnlySpan<int> neighbors)
    {
        var count = 0;
        foreach (var n in neighbors)
        {
            if (n >= 0)
            {
                count++;
            }
        }
        return count;
    }

    private readonly struct Stone : ICellContent
    {
        public char Symbol => 'X';

        public bool Equals(ICellContent? other) => other is Stone;
    }
}
