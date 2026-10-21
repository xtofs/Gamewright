namespace Gamewright;

public readonly record struct Color(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public uint Rgba => (uint)(R | (G << 8) | (B << 16) | (A << 24));
}
