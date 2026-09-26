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
        Assert.Equal((GridCornerDirection)direction, corner);
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

            Assert.Equal(expected, _grid.Neighbors(index).Length);
        }
    }

    [Fact]
    public void Neighbors_TotalLinks_MatchesBoundaryDeficit()
    {
        // every cell has 6 neighbor slots; each of the 6 corners misses 3 and
        // each of the 6*(R-1) other boundary cells misses 2, i.e. 6*(2R+1) in total
        var expected = 6 * _grid.Count - 6 * (2 * Radius + 1);

        var actual = Enumerable.Range(0, _grid.Count).Sum(i => _grid.Neighbors(i).Length);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Neighbors_AreSymmetric()
    {
        for (var a = 0; a < _grid.Count; a++)
        {
            foreach (var b in _grid.Neighbors(a))
            {
                Assert.Contains((short)a, _grid.Neighbors(b).ToArray());
            }
        }
    }

    [Fact]
    public void Neighbors_MatchCubeNeighbor()
    {
        foreach (var (cube, index) in CubeMath.EnumerateGridCoords(Radius))
        {
            var expected = Enum.GetValues<HexDirection>()
                .Select(cube.Neighbor)
                .Where(neighbor => neighbor.IsWithin(Radius))
                .Select(neighbor => (short)neighbor.GetIndex())
                .Order();

            Assert.Equal(expected, _grid.Neighbors(index).ToArray().Order());
        }
    }

    [Fact]
    public void Contains_MatchesTryGet()
    {
        var inside = new CubeCoord(Radius, -Radius, 0);
        var outside = new CubeCoord(Radius + 1, -Radius, -1);
        _grid.Place(inside, Stone.X);

        Assert.True(_grid.Contains(inside));
        Assert.True(_grid.TryGet(inside, out _));
        Assert.False(_grid.Contains(outside));
        Assert.False(_grid.TryGet(outside, out _));
    }

    [Fact]
    public void NewGrid_IsEmpty()
    {
        Assert.All(_grid.Coords, cube => Assert.False(_grid.IsOccupied(cube)));
        Assert.Null(_grid[new CubeCoord(0, 0, 0)]);
    }

    [Fact]
    public void Place_ThenRemove_EmptiesTheCell()
    {
        var cube = new CubeCoord(1, -1, 0);

        _grid.Place(cube, Stone.O);
        Assert.Equal(Stone.O, _grid[cube]);

        Assert.True(_grid.Remove(cube));
        Assert.False(_grid.IsOccupied(cube));
        Assert.False(_grid.Remove(cube));
    }

    [Fact]
    public void Place_DefaultValue_IsStillOccupied()
    {
        var cube = new CubeCoord(0, 0, 0);

        _grid.Place(cube, default);

        Assert.True(_grid.TryGet(cube, out var stone));
        Assert.Equal(Stone.X, stone);
    }

    [Fact]
    public void Move_TransfersThePiece()
    {
        var from = new CubeCoord(0, 0, 0);
        var to = new CubeCoord(0, 1, -1);
        _grid.Place(from, Stone.O);

        _grid.Move(from, to);

        Assert.False(_grid.IsOccupied(from));
        Assert.Equal(Stone.O, _grid[to]);
    }

    [Fact]
    public void Place_OutsideTheGrid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Place(new CubeCoord(Radius + 1, -Radius, -1), Stone.X));
    }

    [Fact]
    public void Format_ShowsPiecesAndEmptyCells()
    {
        // the center of a radius 1 grid is the middle cell of the middle row
        var grid = new HexGrid<Stone>(1);
        char Center() => grid.Format(stone => stone == Stone.X ? 'X' : 'O').Split('\n')[1][2];

        Assert.Equal('.', Center());

        grid.Place(new CubeCoord(0, 0, 0), Stone.O);
        Assert.Equal('O', Center());
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
        Assert.Equal(1 << (int)GridEdgeDirection.North, tracker.EdgeMask(index));
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

        Assert.Equal(1 << (int)GridEdgeDirection.North, tracker.EdgeMask(a.GetIndex()));
        Assert.Equal(0, tracker.CornerMask(a.GetIndex()));
        Assert.Equal(1 << (int)GridCornerDirection.NorthEast, tracker.CornerMask(b.GetIndex()));
        Assert.Equal(0, tracker.EdgeMask(b.GetIndex()));
    }

    private void Place(HexRegionTracker<char> tracker, CubeCoord cube, char group)
    {
        var index = cube.GetIndex();
        tracker.Register(index, group, _grid.EdgeMask(index), _grid.CornerMask(index), _grid.Neighbors(index));
    }

    private enum Stone
    {
        X,
        O,
    }
}
