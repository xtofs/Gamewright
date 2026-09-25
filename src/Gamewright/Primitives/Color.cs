namespace Gamewright;

using System.Numerics;

public readonly struct Color(Vector4 components)
{
    private readonly Vector4 _components = components;

    public Vector4 Vector4 => _components;

    public float R => _components.X;
    public float G => _components.Y;
    public float B => _components.Z;
    public float A => _components.W;

    public Color(float R, float G, float B, float A) : this(new Vector4(R, G, B, A)) { }

    public Color(uint argb) : this(Convert(argb)) { }

    public static Color FromARGB(uint argb)
    {
        return new Color(Convert(argb));
    }

    private static Vector4 Convert(uint rgba)
    {
        // 0xFFFF0000 is red, so the bytes are ARGB (Alpha, Red, Green, Blue) but the Vector4 expects RGBA order.
        return new Vector4(
            ((rgba >> 0x10) & 0xFF) / 255f,
            ((rgba >> 0x08) & 0xFF) / 255f,
            ((rgba >> 0x00) & 0xFF) / 255f,
            ((rgba >> 0x18) & 0xFF) / 255f);
    }

    public Color WithAlpha(float alpha)
    {
        return new Color(R, G, B, alpha);
    }

    public Color? Darken(float percentage)
    {
        return new Color(R * (1 - percentage), G * (1 - percentage), B * (1 - percentage), A);
    }
}
