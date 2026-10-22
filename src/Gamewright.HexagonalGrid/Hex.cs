namespace Gamewright.HexagonalGrid;

public readonly record struct Hex
{
    public readonly int Q { get; }
    public readonly int R { get; }
    public readonly int S => -Q - R; // Derived coordinate for hexagonal grid to ensure q + r + s = 0

    /// <summary>
    /// Gets the index of the hexagon in the hexagonal grid based on its cube coordinates.
    /// The index is stable and grows from the center of the grid outward 
    /// and around each ring in a clockwise manner.
    /// </summary>
    public int Index => HexMath.IndexFromHex(Q, R, S);

    /// <summary>
    /// One of the three color classes (0,1,2) of the hexagon based on its cube coordinates.
    /// The color class is so that no two adjacent hexagons share the same color class.
    /// The center hexagon has a color class of 0.
    /// </summary>
    public int ColorClass => Math.Abs(this.Q % 2) + 2 * Math.Abs(this.R % 2);

    public int Ring => Math.Max(Math.Abs(Q), Math.Max(Math.Abs(R), Math.Abs(S)));

    public Hex(int q, int r, int s)
    {
        if (q + r + s != 0) throw new ArgumentException("q + r + s must be 0");
        Q = q;
        R = r;
        // S is derived from Q and R to maintain the invariant Q + R + S = 0
    }

    public static Hex FromIndex(int index)
    {
        var (q, r, s) = HexMath.HexFromIndex(index);
        return new Hex(q, r, s);
    }

    public readonly void Deconstruct(out int q, out int r, out int s)
    {
        q = Q;
        r = R;
        s = S;
    }

    public static implicit operator Hex((int q, int r, int s) tuple) => new Hex(tuple.q, tuple.r, tuple.s);
    public static implicit operator (int q, int r, int s)(Hex hex) => (hex.Q, hex.R, hex.S);

}
