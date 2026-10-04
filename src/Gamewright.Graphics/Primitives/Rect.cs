namespace Gamewright.Graphics;

using System.Numerics;

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public Rect(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y)
    {
    }

    public Vector2 Position => new(X, Y);

    public Vector2 Size => new(Width, Height);

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

    public static Rect FromCenter(Vector2 center, Vector2 cardSize)
    {
        return new Rect(center - cardSize / 2, cardSize);
    }

    public static Rect operator +(Rect rect, Vector2 offset)
        => new(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
}
