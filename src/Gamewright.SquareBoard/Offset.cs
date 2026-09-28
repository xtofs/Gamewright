namespace Gamewright.SquareBoard;

/// <summary>
/// The displacement from one <see cref="Coordinate"/> to another, e.g. a number of steps in a <see cref="Direction"/>.
/// </summary>
public readonly record struct Offset(int Columns, int Rows)
{
    public static Offset Zero { get; } = new(0, 0);

    public static Offset operator +(Offset a, Offset b) => new(a.Columns + b.Columns, a.Rows + b.Rows);

    public static Offset operator -(Offset a, Offset b) => new(a.Columns - b.Columns, a.Rows - b.Rows);

    public static Offset operator -(Offset a) => new(-a.Columns, -a.Rows);

    public static Offset operator *(int multiplier, Offset a) => new(multiplier * a.Columns, multiplier * a.Rows);

    public static Offset operator *(Offset a, int multiplier) => multiplier * a;

    public static Coordinate operator +(Coordinate square, Offset offset) => new(square.Column + offset.Columns, square.Row + offset.Rows);

    public static Coordinate operator -(Coordinate square, Offset offset) => square + -offset;

    public static implicit operator Offset(Direction direction) => direction.ToOffset();
}

public static class OffsetExtensions
{
    extension(Coordinate)
    {
        /// <summary>The offset that leads from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static Offset operator -(Coordinate to, Coordinate from) => new(to.Column - from.Column, to.Row - from.Row);
    }
}
