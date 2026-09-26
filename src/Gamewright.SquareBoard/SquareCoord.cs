namespace Gamewright.SquareBoard;

/// <summary>
/// A square on a rectangular board, counted from 0.
/// Files are columns (a, b, c, ... in chess), ranks are rows (1, 2, 3, ...).
/// </summary>
public readonly record struct SquareCoord(int File, int Rank)
{
    public static SquareCoord operator +(SquareCoord a, SquareCoord b) => new(a.File + b.File, a.Rank + b.Rank);

    public static SquareCoord operator *(int k, SquareCoord a) => new(k * a.File, k * a.Rank);

    /// <summary>The offset of one step in <paramref name="direction"/>.</summary>
    public static SquareCoord Offset(SquareDirection direction) => direction switch
    {
        SquareDirection.North => new(0, 1),
        SquareDirection.NorthEast => new(1, 1),
        SquareDirection.East => new(1, 0),
        SquareDirection.SouthEast => new(1, -1),
        SquareDirection.South => new(0, -1),
        SquareDirection.SouthWest => new(-1, -1),
        SquareDirection.West => new(-1, 0),
        SquareDirection.NorthWest => new(-1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    public SquareCoord Neighbor(SquareDirection direction) => this + Offset(direction);

    public override string ToString() => $"({File}, {Rank})";
}
