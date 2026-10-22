namespace Gamewright.HexagonalGrid.Tests;

public class HexGridTests
{
    private enum Piece { Red, Blue }

    private static readonly Hex Center = (0, 0, 0);

    [Fact]
    public void ANewGridIsEmpty()
    {
        var grid = new HexGrid<Piece>(4);

        Assert.Equal(HexMath.HexagonCount(4), grid.Count);
        Assert.All(HexMath.EnumerateHexes(4), pair => Assert.Null(grid[pair.Item1]));
        Assert.False(grid.IsOccupied(Center));
    }

    [Fact]
    public void PlacedPiecesAreFoundByCell()
    {
        var grid = new HexGrid<Piece>(4);

        grid.Place(Center, Piece.Blue);
        grid.Place((1, -1, 0), Piece.Red);

        Assert.Equal(Piece.Blue, grid[Center]);
        Assert.Equal(Piece.Red, grid[(1, -1, 0)]);
        Assert.True(grid.IsOccupied(Center));
        Assert.True(grid.TryGet(Center, out var piece));
        Assert.Equal(Piece.Blue, piece);
    }

    [Fact]
    public void ThePieceDefaultIsNotConfusedWithEmpty()
    {
        var grid = new HexGrid<Piece>(3);

        grid.Place(Center, default); // Piece.Red is the default value

        Assert.Equal(Piece.Red, grid[Center]);
        Assert.True(grid.IsOccupied(Center));
        Assert.Null(grid[(1, -1, 0)]);
    }

    [Fact]
    public void CellsOutsideTheBoardAreRefused()
    {
        var grid = new HexGrid<Piece>(3);
        Hex outside = (3, -3, 0);

        Assert.False(grid.Contains(outside));
        Assert.False(grid.TryGet(outside, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Place(outside, Piece.Red));
    }

    [Fact]
    public void RemoveEmptiesACellAndReportsWhetherItWasOccupied()
    {
        var grid = new HexGrid<Piece>(3);
        grid.Place(Center, Piece.Red);

        Assert.True(grid.Remove(Center));
        Assert.False(grid.Remove(Center));
        Assert.Null(grid[Center]);
    }

    [Fact]
    public void MoveTransfersAPieceAndReplacesWhateverIsThere()
    {
        var grid = new HexGrid<Piece>(3);
        grid.Place(Center, Piece.Red);
        grid.Place((1, -1, 0), Piece.Blue);

        grid.Move(Center, (1, -1, 0));

        Assert.Null(grid[Center]);
        Assert.Equal(Piece.Red, grid[(1, -1, 0)]);
        Assert.Throws<InvalidOperationException>(() => grid.Move(Center, (0, 1, -1)));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void ABoardHasSixCornersAndSixTimesRadiusMinusTwoEdgeCells(int radius)
    {
        var grid = new HexGrid<Piece>(radius);
        var corners = new List<GridCorner>();
        var edges = new List<GridEdge>();

        foreach (var (hex, _) in HexMath.EnumerateHexes(radius))
        {
            if (grid.TryGetCorner(hex, out var corner)) corners.Add(corner);
            if (grid.TryGetEdge(hex, out var edge)) edges.Add(edge);
        }

        Assert.Equal(6, corners.Count);
        Assert.Equal(6, corners.Distinct().Count());
        Assert.Equal(6 * (radius - 2), edges.Count);
        Assert.All(edges.GroupBy(e => e), group => Assert.Equal(radius - 2, group.Count()));
    }

    [Fact]
    public void ACornerCellIsNotAnEdgeCell()
    {
        var grid = new HexGrid<Piece>(5);

        Assert.True(grid.TryGetCorner((4, -4, 0), out _));
        Assert.False(grid.TryGetEdge((4, -4, 0), out _));
    }

    private static List<Hex> NeighborsOf(HexGrid<Piece> grid, Hex hex)
    {
        var neighbors = new List<Hex>();
        foreach (var neighbor in grid.Neighbors(hex))
            neighbors.Add(neighbor);
        return neighbors;
    }

    [Fact]
    public void NeighborsStayOnTheBoard()
    {
        var grid = new HexGrid<Piece>(4);

        Assert.Equal(6, NeighborsOf(grid, Center).Count);
        Assert.Equal(3, NeighborsOf(grid, (3, -3, 0)).Count);
        Assert.All(NeighborsOf(grid, (3, -3, 0)), n => Assert.True(grid.Contains(n)));
    }
}
