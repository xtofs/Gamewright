namespace HexLib;


public readonly struct Occupancy : ICellContent
{
    public static readonly Occupancy Empty = new Occupancy('.');
    public static readonly Occupancy P1 = new Occupancy('X');
    public static readonly Occupancy P2 = new Occupancy('O');

    public char Symbol { get; }

    private Occupancy(char symbol)
    {
        Symbol = symbol;
    }

    public bool Equals(ICellContent? other)
    {
        return other is Occupancy occupancy && this.Symbol == occupancy.Symbol;
    }

    public override bool Equals(object? obj)
    {
        return obj is ICellContent content && Equals(content);
    }

    public override int GetHashCode() => Symbol.GetHashCode();
}

