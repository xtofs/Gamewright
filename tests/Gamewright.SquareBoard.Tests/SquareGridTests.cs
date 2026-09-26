namespace Gamewright.SquareBoard.Tests;

public class SquareDirectionTests
{
    public static TheoryData<SquareDirection> Directions() => new(Enum.GetValues<SquareDirection>());

    [Theory]
    [MemberData(nameof(Directions))]
    public void Offset_OfOpposite_CancelsOut(SquareDirection direction)
    {
        var sum = SquareCoord.Offset(direction) + SquareCoord.Offset(direction.Opposite());

        Assert.Equal(new SquareCoord(0, 0), sum);
    }

    [Fact]
    public void OrthogonalAndDiagonal_PartitionAll()
    {
        Assert.Equal(
            SquareDirections.All.Order(),
            SquareDirections.Orthogonal.Concat(SquareDirections.Diagonal).Order());
    }
}

public class SquareGridTests
{
    private readonly SquareGrid<Stone> _grid = new(8, 6);

    [Fact]
    public void Coords_CoverEverySquareOnce()
    {
        Assert.Equal(48, _grid.Coords.Distinct().Count());
        Assert.All(_grid.Coords, square => Assert.True(_grid.Contains(square)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(8, 0)]
    [InlineData(0, 6)]
    public void Contains_IsFalseOutsideTheGrid(int file, int rank)
    {
        var square = new SquareCoord(file, rank);

        Assert.False(_grid.Contains(square));
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
        var square = new SquareCoord(3, 4);

        _grid.Place(square, default);

        Assert.True(_grid.TryGet(square, out var stone));
        Assert.Equal(Stone.X, stone);
    }

    [Fact]
    public void Place_ThenRemove_EmptiesTheSquare()
    {
        var square = new SquareCoord(7, 5);

        _grid.Place(square, Stone.O);
        Assert.Equal(Stone.O, _grid[square]);

        Assert.True(_grid.Remove(square));
        Assert.Null(_grid[square]);
        Assert.False(_grid.Remove(square));
    }

    [Fact]
    public void Move_TransfersThePiece()
    {
        var from = new SquareCoord(0, 0);
        var to = new SquareCoord(2, 2);
        _grid.Place(from, Stone.O);

        _grid.Move(from, to);

        Assert.False(_grid.IsOccupied(from));
        Assert.Equal(Stone.O, _grid[to]);
    }

    [Fact]
    public void Ray_StopsAtTheEdge()
    {
        var ray = _grid.Ray(new SquareCoord(1, 1), SquareDirection.NorthEast);

        Assert.Equal([new(2, 2), new(3, 3), new(4, 4), new(5, 5)], ray);
    }

    [Fact]
    public void Ray_FromTheEdgeOutwards_IsEmpty()
    {
        Assert.Empty(_grid.Ray(new SquareCoord(0, 3), SquareDirection.West));
    }

    private enum Stone
    {
        X,
        O,
    }
}
