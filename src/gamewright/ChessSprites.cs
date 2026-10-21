namespace Gamewright;

public enum PieceKind
{
    King,
    Queen,
    Rook,
    Bishop,
    Knight,
    Pawn
}

public enum PieceColor
{
    White,
    Black
}

/// <summary>Where the chess pieces are on the sprite sheet: white in row 2, black in row 3, in <see cref="PieceKind"/> order.</summary>
public static class ChessSprites
{
    private const int WhiteRow = 2;
    private const int BlackRow = 3;

    public static int Index(PieceColor color, PieceKind kind)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        return SpriteSheet.Index(color == PieceColor.White ? WhiteRow : BlackRow, (int)kind);
    }
}
