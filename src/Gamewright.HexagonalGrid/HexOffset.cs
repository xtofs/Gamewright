namespace Gamewright.HexagonalGrid;

public readonly record struct HexOffset
{
    public HexOffset(int q, int r, int s)
    {
        if (q + r + s != 0) throw new ArgumentException("q + r + s must be 0");
        Q = q;
        R = r;
    }

    public readonly int Q { get; }
    public readonly int R { get; }
    public readonly int S => -Q - R; // Derived coordinate for hexagonal grid to ensure q + r + s = 0

    public static HexOffset Zero => new HexOffset(0, 0, 0);

    public static Hex operator +(Hex hex, HexOffset distance) => new Hex(hex.Q + distance.Q, hex.R + distance.R, hex.S + distance.S);
}


