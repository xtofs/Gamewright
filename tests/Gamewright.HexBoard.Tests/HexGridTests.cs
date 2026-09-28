namespace Gamewright.HexBoard.Tests;

public class DirectionTests
{
    public static TheoryData<Direction> AllDirections() => new(Directions.All);

    [Theory]
    [MemberData(nameof(AllDirections))]
    public void Offset_IsOneStep(Direction direction)
    {
        Assert.Equal(1, direction.ToOffset().Length);
    }

    [Theory]
    [MemberData(nameof(AllDirections))]
    public void Offset_OfOpposite_CancelsOut(Direction direction)
    {
        var sum = direction.ToOffset() + direction.Opposite().ToOffset();

        Assert.Equal(Offset.Zero, sum);
    }

    [Fact]
    public void Offsets_AreDistinct()
    {
        var offsets = Directions.All.Select(direction => direction.ToOffset()).Distinct();

        Assert.Equal(6, offsets.Count());
    }

    [Theory]
    [MemberData(nameof(AllDirections))]
    public void RadiusStepsFromTheCenter_IsTheCornerWithTheSameDirection(Direction direction)
    {
        const int Radius = 4;
        var hex = Coordinate.Center + Radius * direction;

        Assert.True(hex.IsCorner(Radius, out var corner));
        Assert.Equal((GridCornerDirection)direction, corner);
    }
}

public class OffsetTests
{
    [Fact]
    public void Difference_LeadsFromOneCoordinateToTheOther()
    {
        var from = new Coordinate(1, -2, 1);
        var to = new Coordinate(-1, 2, -1);

        Assert.Equal(to, from + (to - from));
        Assert.Equal(from, to - (to - from));
    }

    [Fact]
    public void Length_IsTheNumberOfStepsBetweenNeighbors()
    {
        var offset = 2 * Direction.East + 3 * Direction.SouthWest;

        Assert.Equal(new Offset(2, 1, -3), offset);
        Assert.Equal(3, offset.Length);
    }

    [Fact]
    public void Negation_IsTheOppositeDirection()
    {
        Assert.Equal(Direction.West.ToOffset(), -Direction.East.ToOffset());
        Assert.Equal(Coordinate.Center + Direction.West, Coordinate.Center - Direction.East);
    }

    [Fact]
    public void Constructor_RejectsComponentsThatDoNotSumToZero()
    {
        Assert.Throws<ArgumentException>(() => new Offset(1, 1, 1));
    }
}

public class CoordinateTests
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
        Assert.Equal(expected, new Coordinate(q, r, s).IsWithin(3));
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
    public void Neighbors_MatchCoordinatePlusDirection()
    {
        foreach (var (cube, index) in CubeMath.EnumerateGridCoords(Radius))
        {
            var expected = Directions.All
                .Select(direction => cube + direction)
                .Where(neighbor => neighbor.IsWithin(Radius))
                .Select(neighbor => (short)neighbor.GetIndex())
                .Order();

            Assert.Equal(expected, _grid.Neighbors(index).ToArray().Order());
        }
    }

    [Fact]
    public void Contains_MatchesTryGet()
    {
        var inside = new Coordinate(Radius, -Radius, 0);
        var outside = new Coordinate(Radius + 1, -Radius, -1);
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
        Assert.Null(_grid[new Coordinate(0, 0, 0)]);
    }

    [Fact]
    public void Place_ThenRemove_EmptiesTheCell()
    {
        var cube = new Coordinate(1, -1, 0);

        _grid.Place(cube, Stone.O);
        Assert.Equal(Stone.O, _grid[cube]);

        Assert.True(_grid.Remove(cube));
        Assert.False(_grid.IsOccupied(cube));
        Assert.False(_grid.Remove(cube));
    }

    [Fact]
    public void Place_DefaultValue_IsStillOccupied()
    {
        var cube = new Coordinate(0, 0, 0);

        _grid.Place(cube, default);

        Assert.True(_grid.TryGet(cube, out var stone));
        Assert.Equal(Stone.X, stone);
    }

    [Fact]
    public void Move_TransfersThePiece()
    {
        var from = new Coordinate(0, 0, 0);
        var to = new Coordinate(0, 1, -1);
        _grid.Place(from, Stone.O);

        _grid.Move(from, to);

        Assert.False(_grid.IsOccupied(from));
        Assert.Equal(Stone.O, _grid[to]);
    }

    [Fact]
    public void Place_OutsideTheGrid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Place(new Coordinate(Radius + 1, -Radius, -1), Stone.X));
    }

    [Fact]
    public void Format_ShowsPiecesAndEmptyCells()
    {
        // the center of a radius 1 grid is the middle cell of the middle row
        var grid = new HexGrid<Stone>(1);
        char Center() => grid.Format(stone => stone == Stone.X ? 'X' : 'O').Split('\n')[1][2];

        Assert.Equal('.', Center());

        grid.Place(new Coordinate(0, 0, 0), Stone.O);
        Assert.Equal('O', Center());
    }

    [Fact]
    public void RegionTracker_ChainAlongNorthEdge_TouchesOnlyNorthEdge()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);

        // the north edge is s == Radius, its corners are at q == -Radius and q == 0
        for (var q = -Radius + 1; q < 0; q++)
        {
            _grid.Place(new Coordinate(q, -q - Radius, Radius), Stone.X);
        }

        var index = new Coordinate(-1, 1 - Radius, Radius).GetIndex();
        Assert.Equal(1 << (int)GridEdgeDirection.North, tracker.EdgeMask(index));
        Assert.Equal(0, tracker.CornerMask(index));
    }

    [Fact]
    public void RegionTracker_DoesNotMergeDifferentGroups()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        var a = new Coordinate(-1, 1 - Radius, Radius);
        var b = a + Direction.East;

        _grid.Place(a, Stone.X);
        _grid.Place(b, Stone.O);

        Assert.Equal(1 << (int)GridEdgeDirection.North, tracker.EdgeMask(a.GetIndex()));
        Assert.Equal(0, tracker.CornerMask(a.GetIndex()));
        Assert.Equal(1 << (int)GridCornerDirection.NorthEast, tracker.CornerMask(b.GetIndex()));
        Assert.Equal(0, tracker.EdgeMask(b.GetIndex()));
    }

    [Fact]
    public void RegionTracker_FormatRegion_ShowsMembersAndAggregateMasks()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        var edge = new Coordinate(-1, 1 - Radius, Radius);
        var corner = edge + Direction.East;

        _grid.Place(edge, Stone.X);
        _grid.Place(corner, Stone.X);

        var output = tracker.FormatRegion(corner.GetIndex());
        Assert.Contains("corners=0x02, bridge=False", output);
        Assert.Contains($"  {edge.GetIndex()} ({edge.Q},{edge.R},{edge.S}): group=X", output);
        Assert.Contains($"  {corner.GetIndex()} ({corner.Q},{corner.R},{corner.S}): group=X", output);
    }

    [Fact]
    public void RegionTracker_FirstDefaultValuedPiece_HasOnlyOneMember()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        var corner = new Coordinate(-Radius, 0, Radius);
        _grid.Place(corner, Stone.X);

        Assert.Equal(1 << (int)GridCornerDirection.NorthWest, tracker.CornerMask(corner.GetIndex()));
        Assert.Equal(2, tracker.FormatRegion(corner.GetIndex()).Split('\n').Length);
        Assert.False(tracker.HasBridge(corner.GetIndex()));
    }

    [Theory]
    [InlineData(Stone.X)]
    [InlineData(Stone.O)]
    public void RegionTracker_ConnectedCorners_FormBridge(Stone piece)
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        var firstCorner = new Coordinate(-Radius, 0, Radius);
        var lastCorner = new Coordinate(0, -Radius, Radius);
        _grid.Place(firstCorner, piece);

        for (var q = -Radius + 1; q < 0; q++)
        {
            var edge = new Coordinate(q, -q - Radius, Radius);
            _grid.Place(edge, piece);
            Assert.False(tracker.HasBridge(edge.GetIndex()));
        }

        _grid.Place(lastCorner, piece);
        Assert.True(tracker.HasBridge(lastCorner.GetIndex()));
        Assert.Equal(
            (1 << (int)GridCornerDirection.NorthWest) | (1 << (int)GridCornerDirection.NorthEast),
            tracker.CornerMask(firstCorner.GetIndex()));

        _grid.Remove(new Coordinate(-2, 2 - Radius, Radius));
        Assert.False(tracker.HasBridge(firstCorner.GetIndex()));
        Assert.False(tracker.HasBridge(lastCorner.GetIndex()));
    }

    [Fact]
    public void RegionTracker_OpponentInterruptsBridge()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        for (var q = -Radius; q <= 0; q++)
        {
            _grid.Place(new Coordinate(q, -q - Radius, Radius), q == -2 ? Stone.O : Stone.X);
        }

        Assert.False(tracker.HasBridge(new Coordinate(-Radius, 0, Radius).GetIndex()));
        Assert.False(tracker.HasBridge(new Coordinate(0, -Radius, Radius).GetIndex()));
    }

    [Fact]
    public void RegionTracker_ThreeDistinctEdges_FormFork()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);
        var center = new Coordinate(0, 0, 0);
        _grid.Place(center, Stone.X);

        for (var distance = 1; distance <= Radius; distance++)
        {
            _grid.Place(new Coordinate(-1, 1 - distance, distance), Stone.X);
            _grid.Place(new Coordinate(1, -distance, distance - 1), Stone.X);
        }

        Assert.False(tracker.HasFork(center.GetIndex()));

        for (var distance = 1; distance <= Radius; distance++)
        {
            _grid.Place(new Coordinate(-distance, 1, distance - 1), Stone.X);
        }

        Assert.True(tracker.HasFork(center.GetIndex()));
        Assert.Equal(0, tracker.CornerMask(center.GetIndex()));
    }

    [Fact]
    public void RegionTracker_EmptyCell_CannotStartRegion()
    {
        var tracker = new HexRegionTracker<Stone>(_grid);

        Assert.Throws<InvalidOperationException>(() => tracker.HasBridge(new Coordinate(0, 0, 0).GetIndex()));
    }

    public enum Stone
    {
        X,
        O,
    }
}
