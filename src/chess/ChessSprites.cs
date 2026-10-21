namespace Chess;

using Gamewright;

/// <summary>The chess sprite sheet (<c>sprites.png</c>) and where the pieces are on it: white in row 2, black in row 3, in <see cref="PieceKind"/> order.</summary>
public static class ChessSprites
{
    public static readonly SpriteSheetLayout Sheet = new(columns: 7, rows: 4, cellSize: 200);

    private const int WhiteRow = 2;
    private const int BlackRow = 3;

    public static int Index(PieceColor color, PieceKind kind)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        return Sheet.Index(color == PieceColor.White ? WhiteRow : BlackRow, (int)kind);
    }
}
