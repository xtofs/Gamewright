namespace Havanna;

public enum Piece : byte
{
    White,

    Black,
}

public static class PieceExtensions
{
    extension(Piece piece)
    {
        public Piece Opponent => piece == Piece.White ? Piece.Black : Piece.White;

        public char Symbol => piece == Piece.White ? 'X' : 'O';
    }
}
