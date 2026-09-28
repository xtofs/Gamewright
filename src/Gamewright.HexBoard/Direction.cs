namespace Gamewright.HexBoard;

/// <summary>
/// The six directions from a pointy-top hexagon to its neighbors, clockwise from NorthWest.
/// </summary>
/// <remarks>
/// The order matches <see cref="GridCornerDirection"/>: the corner of a grid with radius R
/// in direction d is <see cref="Coordinate.Center"/> + R * d.
/// Unlike <see cref="GridEdgeDirection"/>, which names the edges of the board,
/// these name the directions between adjacent cells.
/// Directions convert to <see cref="Offset"/>s and can be added to <see cref="Coordinate"/>s.
/// </remarks>
public enum Direction
{
    NorthWest, NorthEast, East, SouthEast, SouthWest, West
}

public static class Directions
{
    /// <summary>All six directions, clockwise from NorthWest.</summary>
    public static IReadOnlyList<Direction> All { get; } = Enum.GetValues<Direction>();

    extension(Direction direction)
    {
        public Direction Opposite() => (Direction)(((int)direction + 3) % 6);
    }

    extension(Direction)
    {
        public static Offset operator *(Direction direction, int multiplier) => direction.ToOffset() * multiplier;

        public static Offset operator *(int multiplier, Direction direction) => multiplier * direction.ToOffset();

        public static Coordinate operator +(Coordinate hex, Direction direction) => hex + direction.ToOffset();

        public static Coordinate operator -(Coordinate hex, Direction direction) => hex - direction.ToOffset();
    }

    extension(Direction direction)
    {
        /// <summary>The offset of one step in the given <paramref name="direction"/>.</summary>
        public Offset ToOffset() => direction switch
        {
            Direction.NorthWest => new Offset(-1, 0, 1),
            Direction.NorthEast => new Offset(0, -1, 1),
            Direction.East => new Offset(1, -1, 0),
            Direction.SouthEast => new Offset(1, 0, -1),
            Direction.SouthWest => new Offset(0, 1, -1),
            Direction.West => new Offset(-1, 1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}
