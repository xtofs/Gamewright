namespace HotReloadSpike.Resources;

using System.Numerics;

/// <summary>
/// Game-side view of a texture: its handle plus the size needed to turn pixel regions into UVs.
/// Holds no GL object.
/// </summary>
public sealed record Texture(TextureId Id, int Width, int Height)
{
    public Vector2 Size => new Vector2(Width, Height);

    /// <summary>Converts a pixel region into (u0, v0, u1, v1).</summary>
    public Vector4 UvOf(Rect region)
        => new Vector4(
            region.X / Width,
            region.Y / Height,
            (region.X + region.Width) / Width,
            (region.Y + region.Height) / Height);
}
