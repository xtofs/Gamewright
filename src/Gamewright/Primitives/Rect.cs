namespace Gamewright;

public record struct Rect(float X, float Y, float Width, float Height)
{
    public Rect(Point topLeft, Size size) : this(topLeft.X, topLeft.Y, size.Width, size.Height)
    {
    }

    public readonly Size Size => new(Width, Height);

    public readonly Point TopLeft => new(X, Y);

    public readonly Point Center => new(X + Width / 2, Y + Height / 2);

    public readonly float Right => X + Width;

    public readonly float Bottom => Y + Height;

    /// <summary>A rectangle of the given size centered on <paramref name="center"/>.</summary>
    public static Rect FromCenter(Point center, Size size) =>
        new(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);

    /// <summary>True when the point lies inside; the right and bottom edges are exclusive.</summary>
    public readonly bool Contains(Point point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    /// <summary>The rectangle grown by <paramref name="dx"/> left and right and <paramref name="dy"/> top and bottom.</summary>
    public readonly Rect Inflate(float dx, float dy) => new(X - dx, Y - dy, Width + 2 * dx, Height + 2 * dy);

    /// <summary>The rectangle shrunk by <paramref name="d"/> on every side.</summary>
    public readonly Rect Deflate(float d) => Inflate(-d, -d);


    public readonly void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    public static implicit operator (float x, float y, float width, float height)(Rect value)
    {
        return (value.X, value.Y, value.Width, value.Height);
    }

    public static implicit operator Rect((float x, float y, float width, float height) value)
    {
        return new Rect(value.x, value.y, value.width, value.height);
    }
}