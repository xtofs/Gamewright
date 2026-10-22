namespace Gamewright;

/// <summary>A two-dimensional extent: <paramref name="Width"/> and <paramref name="Height"/>.</summary>
public readonly record struct Size(float Width, float Height)
{
    public static Size Empty => default;

    public static Size Square(float side) => new(side, side);

    public Displacement ToDisplacement() => new(Width, Height);

    public static Size operator *(Size size, float factor) => new(size.Width * factor, size.Height * factor);

    public static Size operator /(Size size, float divisor) => new(size.Width / divisor, size.Height / divisor);
}
