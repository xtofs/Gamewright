namespace Gamewright;

public record struct Rect(float X, float Y, float Width, float Height)
{

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