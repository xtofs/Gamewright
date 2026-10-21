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

public readonly record struct Piece(PieceColor Color, PieceKind Kind);
