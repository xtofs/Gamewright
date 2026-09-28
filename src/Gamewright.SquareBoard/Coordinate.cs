namespace Gamewright.SquareBoard;

/// <summary>
/// A square on a rectangular board, counted from 0.
/// Columns are counted from 0 and are increasing to the right,
/// and Rows are counted from 0 and increase downwards.
/// In Chess Columns are called Files (a, b, c, ... in chess), and Rows are called Ranks (1, 2, 3, ...).
/// </summary>
public readonly record struct Coordinate(int Column, int Row)
{
    public static Coordinate operator +(Coordinate a, Coordinate b) => new(a.Column + b.Column, a.Row + b.Row);

    public static Coordinate operator *(int k, Coordinate a) => new(k * a.Column, k * a.Row);

    /// <summary>The offset of one step in <paramref name="direction"/>.</summary>
    public static Coordinate Offset(Direction direction) => direction switch
    {
        Direction.North => new(0, 1),
        Direction.NorthEast => new(1, 1),
        Direction.East => new(1, 0),
        Direction.SouthEast => new(1, -1),
        Direction.South => new(0, -1),
        Direction.SouthWest => new(-1, -1),
        Direction.West => new(-1, 0),
        Direction.NorthWest => new(-1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    public Coordinate Neighbor(Direction direction) => this + direction.ToOffset();

    public override string ToString() => $"({Column}, {Row})";
}


