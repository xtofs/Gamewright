namespace Havannah;



public enum HavannahPiece
{
    Red, Blue
}

public static class HavannahPieceExtensions
{
    extension(HavannahPiece piece)
    {
        public HavannahPiece Opponent => piece switch
        {
            HavannahPiece.Red => HavannahPiece.Blue,
            HavannahPiece.Blue => HavannahPiece.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, null),
        };
    }
}
