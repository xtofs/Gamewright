namespace Gamewright;

public readonly record struct Color(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public uint Rgba => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    /// <summary>The same color with its alpha multiplied by <paramref name="factor"/> (0 transparent, 1 unchanged).</summary>
    public Color WithAlpha(float factor)
    {
        if (!float.IsFinite(factor))
            throw new ArgumentOutOfRangeException(nameof(factor), "Alpha factor must be finite.");
        return this with { A = (byte)Math.Clamp(MathF.Round(A * factor, MidpointRounding.AwayFromZero), 0, 255) };
    }
}
