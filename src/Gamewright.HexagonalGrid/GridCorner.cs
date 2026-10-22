namespace Gamewright.HexagonalGrid;

/// <summary>
/// Represents a corner of the pointy-top hexagon hexagonal board .
/// Pointy-top hexagons lead to a flat top board.
/// The enum uses the direction of the hours on a watch face (1,) to name the corners.
/// </summary>
public enum GridCorner
{
    OneOClock,
    ThreeOClock,
    FiveOClock,
    SevenOClock,
    NineOClock,
    ElevenOClock
}

public static class GridCornerExtensions
{
    extension(GridCorner corner)
    {
        public GridCorner Opposite() => corner switch
        {
            GridCorner.OneOClock => GridCorner.SevenOClock,
            GridCorner.ThreeOClock => GridCorner.NineOClock,
            GridCorner.FiveOClock => GridCorner.ElevenOClock,
            GridCorner.SevenOClock => GridCorner.OneOClock,
            GridCorner.NineOClock => GridCorner.ThreeOClock,
            GridCorner.ElevenOClock => GridCorner.FiveOClock,
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
        };

    }
}
