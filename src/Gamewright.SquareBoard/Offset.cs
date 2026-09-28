namespace Gamewright.SquareBoard;

public record Offset(int Columns, int Rows)
{
    public static Coordinate operator +(Coordinate a, Offset b) => new(a.Column + b.Columns, a.Row + b.Rows);

    public static Offset operator *(int multiplier, Offset a) => new(multiplier * a.Columns, multiplier * a.Rows);
    public static Offset operator *(Offset a, int multiplier) => new(multiplier * a.Columns, multiplier * a.Rows);
    public static Offset operator +(Offset a, Offset b) => new(a.Columns + b.Columns, a.Rows + b.Rows);

    public static implicit operator Offset(Direction direction) => direction.ToOffset();
}
