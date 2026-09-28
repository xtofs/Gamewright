namespace Gamewright.SquareBoard;

/// <summary>
/// A square on a rectangular board, counted from 0.
/// Columns are counted from 0 and are increasing to the right,
/// and Rows are counted from 0 and increase downwards.
/// </summary>
/// <remarks>
/// In Chess Columns are called Files (a, b, c, ... in chess), and Rows are called Ranks (1, 2, 3, ...).
/// See <see cref="Direction"/> and <see cref="Offset"/> for Coordinate arithmetic.
/// </remarks>
public readonly record struct Coordinate(int Column, int Row)
{
    public override string ToString() => $"({Column}, {Row})";
}
