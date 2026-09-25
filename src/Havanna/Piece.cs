namespace Havanna;

using Gamewright.HexBoard;

public readonly struct Piece : ICellContent
{
    public Piece() : this('X') { }

    public static readonly Piece White = new Piece('X');
    public static readonly Piece Black = new Piece('O');

    public static IEnumerable<Piece> GetValues() => [White, Black];

    public char Symbol { get; }

    private Piece(char symbol)
    {
        Symbol = symbol;
    }

    public Piece Opponent => this == White ? Black : White;

    public override string ToString()
    {
        return $"{{Piece {Symbol}}}";
    }

    public bool Equals(ICellContent? other)
    {
        return other is Piece occupancy && this.Symbol == occupancy.Symbol;
    }

    public override bool Equals(object? obj)
    {
        return obj is ICellContent content && Equals(content);
    }

    public override int GetHashCode() => Symbol.GetHashCode();

    public static bool operator ==(Piece left, Piece right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Piece left, Piece right)
    {
        return !(left == right);
    }
}

