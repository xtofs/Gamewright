namespace HotReloadSpike.Resources;

using System.Numerics;

/// <summary>A rectangle in pixels, e.g. a region of a sprite sheet.</summary>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public Vector2 Position => new Vector2(X, Y);

    public Vector2 Size => new Vector2(Width, Height);
}
