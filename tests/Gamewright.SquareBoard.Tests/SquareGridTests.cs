namespace Gamewright.SquareBoard.Tests;

public class SquareDirectionTests
{
    public static TheoryData<Direction> AllDirections() => new(Enum.GetValues<Direction>());

    [Theory]
    [MemberData(nameof(AllDirections))]
    public void Offset_OfOpposite_CancelsOut(Direction direction)
    {
        var sum = direction.ToOffset() + direction.Opposite().ToOffset();

        Assert.Equal(new Offset(0, 0), sum);
    }

    [Fact]
    public void North_DecreasesRow_And_East_IncreasesColumn()
    {
        var square = new Coordinate(3, 3);

        Assert.Equal(new Coordinate(3, 2), square + Direction.North);
        Assert.Equal(new Coordinate(4, 3), square + Direction.East);
    }

    [Fact]
    public void OrthogonalAndDiagonal_PartitionAll()
    {
        Assert.Equal(
            Directions.OrthogonalAndDiagonal.Order(),
            Directions.Orthogonal.Concat(Directions.Diagonal).Order());
    }
}

public class SquareGridTests
{
    private readonly CheckerBoard<Stone> _grid = new(8, 6);

    [Fact]
    public void Coords_CoverEverySquareOnce()
    {
        Assert.Equal(48, _grid.Coords.Distinct().Count());
        Assert.All(_grid.Coords, square => Assert.True(_grid.IsValid(square)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(8, 0)]
    [InlineData(0, 6)]
    public void IsValid_IsFalseOutsideTheGrid(int file, int rank)
    {
        var square = new Coordinate(file, rank);

        Assert.False(_grid.IsValid(square));
        Assert.False(_grid.TryGet(square, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Place(square, Stone.X));
    }

    [Fact]
    public void NewGrid_IsEmpty()
    {
        Assert.All(_grid.Coords, square => Assert.False(_grid.IsOccupied(square)));
    }

    [Fact]
    public void Place_DefaultValue_IsStillOccupied()
    {
        var square = new Coordinate(3, 4);

        _grid.Place(square, default);

        Assert.True(_grid.TryGet(square, out var stone));
        Assert.Equal(Stone.X, stone);
    }

    [Fact]
    public void Place_ThenRemove_EmptiesTheSquare()
    {
        var square = new Coordinate(7, 5);

        _grid.Place(square, Stone.O);
        Assert.Equal(Stone.O, _grid[square]);

        Assert.True(_grid.Remove(square));
        Assert.Null(_grid[square]);
        Assert.False(_grid.Remove(square));
    }

    [Fact]
    public void Move_TransfersThePiece()
    {
        var from = new Coordinate(0, 0);
        var to = new Coordinate(2, 2);
        _grid.Place(from, Stone.O);

        _grid.Move(from, to);

        Assert.False(_grid.IsOccupied(from));
        Assert.Equal(Stone.O, _grid[to]);
    }

    [Fact]
    public void Ray_StopsAtTheEdge()
    {
        var ray = _grid.Ray(new Coordinate(1, 1), Direction.SouthEast);

        Assert.Equal([new(2, 2), new(3, 3), new(4, 4), new(5, 5)], ray);
    }

    [Fact]
    public void Ray_FromTheEdgeOutwards_IsEmpty()
    {
        Assert.Empty(_grid.Ray(new Coordinate(0, 3), Direction.West));
    }

    private enum Stone
    {
        X,
        O,
    }
}
