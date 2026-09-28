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


    // [Obsolete("Use the '+' operator with Direction.ToOffset() instead.")]
    // public Coordinate Neighbor(Direction direction) => this + direction.ToOffset();

    public override string ToString() => $"({Column}, {Row})";
}


