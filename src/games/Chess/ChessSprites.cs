namespace Chess;

using Gamewright;

/// <summary>Where the chess pieces are on the sprite image (<c>sprites.png</c>): white in row 2, black in row 3, in <see cref="PieceKind"/> order.</summary>
public static class ChessSprites
{
    public static readonly Image Image = new("sprites.png");

    /// <summary>Width and height of one cell of the image, in pixels.</summary>
    public const int CellSize = 200;

    private const int WhiteRow = 2;
    private const int BlackRow = 3;

    /// <summary>The column and row of a piece's cell.</summary>
    public static (int Column, int Row) Locate(Piece piece)
    {
        if (!Enum.IsDefined(piece.Color))
            throw new ArgumentOutOfRangeException(nameof(piece));
        if (!Enum.IsDefined(piece.Kind))
            throw new ArgumentOutOfRangeException(nameof(piece));

        return ((int)piece.Kind, piece.Color == PieceColor.White ? WhiteRow : BlackRow);
    }
}
