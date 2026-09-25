namespace Gamewright;

using System.Numerics;

public readonly record struct TextureRegion(Vector2 Minimum, Vector2 Maximum)
{
    public static TextureRegion Full { get; } = new(Vector2.Zero, Vector2.One);
}
