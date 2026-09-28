namespace Gamewright.SquareBoard;

/// <summary>
/// The eight directions from a square to its neighbors, clockwise from North.
/// South is increasing Row, East is increasing Column.
/// See Offset for the corresponding row and column changes.
/// Directions can be converted to Offsets and added to Coordinates.
/// </summary>
public enum Direction
{
    North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
}

public static class Directions
{
    /// <summary>The four directions a rook moves in.</summary>
    public static IReadOnlyList<Direction> Orthogonal { get; } =
        [Direction.North, Direction.East, Direction.South, Direction.West];

    /// <summary>The four directions a bishop moves in.</summary>
    public static IReadOnlyList<Direction> Diagonal { get; } =
        [Direction.NorthEast, Direction.SouthEast, Direction.SouthWest, Direction.NorthWest];

    /// <summary>All eight directions, the ones a queen moves in.</summary>
    public static IReadOnlyList<Direction> OrthogonalAndDiagonal { get; } = Enum.GetValues<Direction>();

    extension(Direction direction)
    {
        public Direction Opposite() => (Direction)(((int)direction + 4) % 8);
    }
}

public static class DirectionExtensions
{
    extension(Direction)
    {
        public static Offset operator *(Direction a, int multiplier) => a.ToOffset() * multiplier;

        public static Offset operator *(int multiplier, Direction a) => a.ToOffset() * multiplier;

        public static Coordinate operator +(Coordinate c, Direction d) => c + d.ToOffset();
    }

    extension(Direction direction)
    {
        public Offset ToOffset() => direction switch
        {
            Direction.North => new Offset(0, -1),
            Direction.NorthEast => new Offset(1, -1),
            Direction.East => new Offset(1, 0),
            Direction.SouthEast => new Offset(1, +1),
            Direction.South => new Offset(0, +1),
            Direction.SouthWest => new Offset(-1, +1),
            Direction.West => new Offset(-1, 0),
            Direction.NorthWest => new Offset(-1, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}
