namespace Gamewright.HexBoard;

/// <summary>
/// The displacement from one <see cref="Coordinate"/> to another, e.g. a number of steps in a <see cref="Direction"/>.
/// </summary>
/// <remarks>
/// Like <see cref="Coordinate"/> it is stored in cube coordinates with Q + R + S = 0.
/// </remarks>
public readonly record struct Offset
{
    public static Offset Zero { get; } = new(0, 0, 0);

    public Offset(int q, int r, int s)
    {
        ArgumentOutOfRangeException.ThrowIfNotZero(q + r + s, "The sum of q, r, and s must be 0.");
        Q = q;
        R = r;
    }

    public int Q { get; }
    public int R { get; }

    // the S is calculated by deriving the formula from the constraint Q + R + S = 0
    public int S => -(Q + R);

    /// <summary>The number of steps between neighbors needed to cover the offset.</summary>
    public int Length => Math.Max(Math.Abs(Q), Math.Abs(R), Math.Abs(S));

    public void Deconstruct(out int q, out int r, out int s)
    {
        q = Q;
        r = R;
        s = S;
    }

    public static Offset operator +(Offset a, Offset b) => new(a.Q + b.Q, a.R + b.R, a.S + b.S);

    public static Offset operator -(Offset a, Offset b) => new(a.Q - b.Q, a.R - b.R, a.S - b.S);

    public static Offset operator -(Offset a) => new(-a.Q, -a.R, -a.S);

    public static Offset operator *(int multiplier, Offset a) => new(multiplier * a.Q, multiplier * a.R, multiplier * a.S);

    public static Offset operator *(Offset a, int multiplier) => multiplier * a;

    public static Coordinate operator +(Coordinate hex, Offset offset) => new(hex.Q + offset.Q, hex.R + offset.R, hex.S + offset.S);

    public static Coordinate operator -(Coordinate hex, Offset offset) => hex + -offset;

    public static implicit operator Offset(Direction direction) => direction.ToOffset();
}

public static class OffsetExtensions
{
    extension(Coordinate)
    {
        /// <summary>The offset that leads from <paramref name="b"/> to <paramref name="a"/>.</summary>
        public static Offset operator -(Coordinate a, Coordinate b) => new(a.Q - b.Q, a.R - b.R, a.S - b.S);
    }
}
