namespace Havanna;

public enum Piece : byte
{
    Red,

    Blue,
}

public static class PieceExtensions
{
    extension(Piece piece)
    {
        public Piece Opponent => piece == Piece.Red ? Piece.Blue : Piece.Red;

        public char Symbol => piece == Piece.Red ? 'X' : 'O';
    }
}
