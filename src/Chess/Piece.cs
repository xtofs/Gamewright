namespace Chess;

public enum Piece : byte
{
    WhiteKing = 0x01,
    WhiteQueen = 0x02,
    WhiteRook = 0x03,
    WhiteBishop = 0x04,
    WhiteKnight = 0x5,
    WhitePawn = 0x06,

    BlackKing = 0x81,
    BlackQueen = 0x82,
    BlackRook = 0x83,
    BlackBishop = 0x84,
    BlackKnight = 0x85,
    BlackPawn = 0x86,
}

public enum Occupancy : byte
{
    None = 0x00,

    WhiteKing = 0x01,
    WhiteQueen = 0x02,
    WhiteRook = 0x03,
    WhiteBishop = 0x04,
    WhiteKnight = 0x5,
    WhitePawn = 0x06,

    BlackKing = 0x81,
    BlackQueen = 0x82,
    BlackRook = 0x83,
    BlackBishop = 0x84,
    BlackKnight = 0x85,
    BlackPawn = 0x86,
}

public static class PieceExtensions
{
    extension(Piece piece)
    {
        public Occupancy ToOccupancy()
        {
            return (Occupancy)piece;
        }
    }
    extension(Occupancy occupancy)
    {
        public bool TryGetPiece(out Piece piece)
        {
            piece = (Piece)occupancy;
            return occupancy != Occupancy.None;
        }
    }
}
