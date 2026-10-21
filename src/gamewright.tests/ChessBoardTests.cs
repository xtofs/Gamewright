using Gamewright;

namespace Gamewright.Tests;

public class ChessBoardTests
{
    private static Square At(string name) => new(name[0] - 'a', ChessBoard.Size - (name[1] - '0'));

    [Fact]
    public void NewGameHasTheStandardSetup()
    {
        var board = ChessBoard.NewGame();

        Assert.Equal(32, board.Pieces().Count());
        Assert.Equal(new Piece(PieceColor.White, PieceKind.King), board[At("e1")]);
        Assert.Equal(new Piece(PieceColor.Black, PieceKind.Queen), board[At("d8")]);
        Assert.Equal(new Piece(PieceColor.White, PieceKind.Pawn), board[At("h2")]);
        Assert.Null(board[At("e4")]);
        Assert.Equal(PieceColor.White, board.Turn);
        Assert.Null(board.Selected);
    }

    [Fact]
    public void SquareNamesAreAlgebraic()
    {
        Assert.Equal("a8", new Square(0, 0).ToString());
        Assert.Equal("h1", new Square(7, 7).ToString());
    }

    [Fact]
    public void SelectingAnOwnPieceSelectsIt()
    {
        var board = ChessBoard.NewGame();

        Assert.Equal(ClickResult.Selected, board.Click(At("e2")));
        Assert.Equal(At("e2"), board.Selected);
    }

    [Theory]
    [InlineData("e4")] // empty
    [InlineData("e7")] // opponent's piece
    public void CannotSelectEmptySquaresOrOpponentPieces(string square)
    {
        var board = ChessBoard.NewGame();

        Assert.Equal(ClickResult.Ignored, board.Click(At(square)));
        Assert.Null(board.Selected);
    }

    [Fact]
    public void ClickingTheSelectedPieceDeselectsIt()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("e2"));

        Assert.Equal(ClickResult.Deselected, board.Click(At("e2")));
        Assert.Null(board.Selected);
    }

    [Fact]
    public void ClickingAnotherOwnPieceChangesTheSelection()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("e2"));

        Assert.Equal(ClickResult.Selected, board.Click(At("d2")));
        Assert.Equal(At("d2"), board.Selected);
        Assert.Equal(PieceColor.White, board.Turn);
    }

    [Fact]
    public void MovingPlacesThePieceAndPassesTheTurn()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("e2"));

        Assert.Equal(ClickResult.Moved, board.Click(At("e4")));

        Assert.Null(board[At("e2")]);
        Assert.Equal(new Piece(PieceColor.White, PieceKind.Pawn), board[At("e4")]);
        Assert.Null(board.Selected);
        Assert.Equal(PieceColor.Black, board.Turn);
        Assert.Equal(new Move(At("e2"), At("e4"), new Piece(PieceColor.White, PieceKind.Pawn), null), board.LastMove);
    }

    [Fact]
    public void AnyDestinationIsAllowed()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("a1"));

        Assert.Equal(ClickResult.Moved, board.Click(At("h5")));
        Assert.Equal(new Piece(PieceColor.White, PieceKind.Rook), board[At("h5")]);
    }

    [Fact]
    public void MovingOntoAnOpponentCaptures()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("d1"));
        board.Click(At("d7"));

        Assert.Equal(new Piece(PieceColor.White, PieceKind.Queen), board[At("d7")]);
        Assert.Equal(31, board.Pieces().Count());
        Assert.Equal(new Piece(PieceColor.Black, PieceKind.Pawn), board.LastMove!.Value.Captured);
    }

    [Fact]
    public void OnlyTheSideToMoveCanSelect()
    {
        var board = ChessBoard.NewGame();
        board.Click(At("e2"));
        board.Click(At("e4"));

        Assert.Equal(ClickResult.Ignored, board.Click(At("d2")));
        Assert.Equal(ClickResult.Selected, board.Click(At("d7")));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 8)]
    public void ClickingOffTheBoardThrows(int file, int rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ChessBoard.NewGame().Click(new Square(file, rank)));
}
