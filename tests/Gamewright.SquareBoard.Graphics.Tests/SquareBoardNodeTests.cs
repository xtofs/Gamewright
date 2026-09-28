namespace Gamewright.SquareBoard.Graphics.Tests;

using System.Numerics;
using Gamewright.Graphics;

public class SquareBoardNodeTests
{
    // 8 squares of 4 units plus two borders of 1 unit = 34 units, 10 px each
    private static readonly Vector2 Size = new(340, 340);

    private static SquareBoardNode CreateBoard(Vector2 size, bool withLabels = true)
    {
        var root = new RootNode();
        var board = root.AddSquareBoard(8, 8, withLabels);
        root.Update(size);
        return board;
    }

    [Fact]
    public void Rank0_IsAtTheBottom_File0_OnTheLeft()
    {
        var board = CreateBoard(Size);

        Assert.Equal(new Rect(10, 290, 40, 40), board.GetSquareAndColor(new Coordinate(0, 0)).Rect);
        Assert.Equal(new Rect(290, 10, 40, 40), board.GetSquareAndColor(new Coordinate(7, 7)).Rect);
    }

    [Fact]
    public void A1_IsDark()
    {
        var board = CreateBoard(Size);

        Assert.Equal(board.Dark, board.GetSquareAndColor(new Coordinate(0, 0)).Color);
        Assert.Equal(board.Light, board.GetSquareAndColor(new Coordinate(7, 0)).Color);
    }

    [Fact]
    public void TryGetSquare_RoundTripsThroughEveryCenter()
    {
        var board = CreateBoard(new Vector2(500, 300));

        foreach (var (file, rank) in Enumerable.Range(0, 8).SelectMany(f => Enumerable.Range(0, 8).Select(r => (f, r))))
        {
            var square = new Coordinate(file, rank);

            Assert.True(board.TryGetSquare(board.GetCenter(square), out var actual));
            Assert.Equal(square, actual);
        }
    }

    [Theory]
    [InlineData(5, 170)]   // left rank labels
    [InlineData(170, 335)] // bottom file labels
    [InlineData(-5, 170)]  // outside the board
    public void TryGetSquare_IsFalseOffTheSquares(float x, float y)
    {
        var board = CreateBoard(Size);

        Assert.False(board.TryGetSquare(new Vector2(x, y), out _));
    }

    [Fact]
    public void Labels_SurroundTheBoard()
    {
        var labels = CreateBoard(Size).GetLabels().ToList();

        Assert.Equal(32, labels.Count);
        Assert.Contains((new Rect(10, 330, 40, 10), "a"), labels);
        Assert.Contains((new Rect(0, 290, 10, 40), "1"), labels);
    }

    [Fact]
    public void WithoutLabels_SquaresFillTheBoard()
    {
        var board = CreateBoard(new Vector2(320, 320), withLabels: false);

        Assert.Empty(board.GetLabels());
        Assert.Equal(new Rect(0, 280, 40, 40), board.GetSquareAndColor(new Coordinate(0, 0)).Rect);
    }
}
