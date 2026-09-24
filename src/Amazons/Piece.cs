namespace Amazons;

public enum Piece : byte
{
    WhiteQueen = 0x01,

    BlackQueen = 0x02,

    Flame = 0x03
}

public enum Occupancy : byte
{
    None = 0x00,

    WhiteQueen = 0x01,

    BlackQueen = 0x02,

    Flame = 0x03
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
