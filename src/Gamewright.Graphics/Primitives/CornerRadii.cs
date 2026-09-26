namespace Gamewright.Graphics;

public readonly record struct CornerRadii(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public CornerRadii(float radius)
        : this(radius, radius, radius, radius)
    {
    }
}
