namespace Gamewright;

public enum ClickResult
{
    /// <summary>Nothing happened: an empty square or an opponent's piece with nothing selected.</summary>
    Ignored,
    Selected,
    Deselected,
    Moved
}

public readonly record struct Move(Square From, Square To, Piece Piece, Piece? Captured);

/// <summary>
/// A chess position that is played by clicking: select one of your pieces, then click any other
/// square to move it there. Moves are not checked against the rules of chess, and capturing is
/// just moving onto an opponent's piece.
/// </summary>
public sealed class ChessBoard
{
    public const int Size = 8;

    private static readonly PieceKind[] BackRank =
        [PieceKind.Rook, PieceKind.Knight, PieceKind.Bishop, PieceKind.Queen,
         PieceKind.King, PieceKind.Bishop, PieceKind.Knight, PieceKind.Rook];

    private readonly Piece?[,] squares = new Piece?[Size, Size];

    public PieceColor Turn { get; private set; } = PieceColor.White;

    public Square? Selected { get; private set; }

    public Move? LastMove { get; private set; }

    public static ChessBoard NewGame()
    {
        var board = new ChessBoard();
        for (var file = 0; file < Size; file++)
        {
            board.squares[file, 0] = new Piece(PieceColor.Black, BackRank[file]);
            board.squares[file, 1] = new Piece(PieceColor.Black, PieceKind.Pawn);
            board.squares[file, 6] = new Piece(PieceColor.White, PieceKind.Pawn);
            board.squares[file, 7] = new Piece(PieceColor.White, BackRank[file]);
        }
        return board;
    }

    public Piece? this[Square square]
    {
        get
        {
            RequireOnBoard(square);
            return squares[square.File, square.Rank];
        }
    }

    public IEnumerable<(Square Square, Piece Piece)> Pieces()
    {
        for (var rank = 0; rank < Size; rank++)
        {
            for (var file = 0; file < Size; file++)
            {
                if (squares[file, rank] is { } piece)
                    yield return (new Square(file, rank), piece);
            }
        }
    }

    public ClickResult Click(Square square)
    {
        RequireOnBoard(square);
        var piece = squares[square.File, square.Rank];
        var ownPiece = piece is { } p && p.Color == Turn;

        if (Selected is not { } from)
        {
            if (!ownPiece)
                return ClickResult.Ignored;
            Selected = square;
            return ClickResult.Selected;
        }

        if (square == from)
        {
            Selected = null;
            return ClickResult.Deselected;
        }
        if (ownPiece)
        {
            Selected = square;
            return ClickResult.Selected;
        }

        var moving = squares[from.File, from.Rank]!.Value;
        squares[square.File, square.Rank] = moving;
        squares[from.File, from.Rank] = null;
        LastMove = new Move(from, square, moving, piece);
        Selected = null;
        Turn = Turn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        return ClickResult.Moved;
    }

    private static void RequireOnBoard(Square square)
    {
        if (!square.IsOnBoard)
            throw new ArgumentOutOfRangeException(nameof(square), square, "Square is not on the board.");
    }
}
