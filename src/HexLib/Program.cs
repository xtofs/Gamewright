using System.Diagnostics;
using HexLib;

internal class Program
{
    private static void Main(string[] args)
    {

        const int R = 5;


        var board = new HexGrid<Token>(R);

        for (var i = 0; i < 1 + 6 + 12 + 18; i++)
        {
            var hex = CubeCoord.FromIndex(i);
            board.PlaceStone(hex, new Token(i));
        }

        Console.WriteLine(board.Format());
    }
}

readonly struct Token : ICellContent
{
    public char Symbol { get; }

    public Token(int n)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, 26 * 2);
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        Symbol = n < 26 ? (char)('a' + n) : (char)('A' + (n - 26));
        Debug.Assert(char.IsAsciiLetter(Symbol));
    }

    public readonly bool Equals(ICellContent? other)
    {
        return other is Token digit && this.Symbol == digit.Symbol;
    }

    public override bool Equals(object? obj)
    {
        return obj is ICellContent content && Equals(content);
    }

    public override readonly int GetHashCode() => Symbol.GetHashCode();
}
