namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;
using HotReloadSpike.Resources;

/// <summary>
/// Draws a region of a texture. Carries the <see cref="TextureId"/> handle, not the texture:
/// the render thread resolves it in <see cref="TextureStore"/>.
/// </summary>
public struct DrawSpriteCommand
{
    public const RenderOp OpCode = RenderOp.DrawSprite;

    public TextureId Texture;
    public Vector4 Destination; // x, y, width, height in pixels
    public Vector4 Uv;          // u0, v0, u1, v1
    public Vector4 Tint;

    public static DrawSpriteCommand Create(TextureId texture, Vector2 position, Vector2 size, Vector4 uv, Vector4 tint)
        => new DrawSpriteCommand
        {
            Texture = texture,
            Destination = new Vector4(position, size.X, size.Y),
            Uv = uv,
            Tint = tint,
        };

    public static void Execute(RenderContext ctx, ref DrawSpriteCommand cmd)
    {
        ctx.AddSprite(cmd.Texture, new SpriteInstance
        {
            Destination = cmd.Destination,
            Uv = cmd.Uv,
            Tint = cmd.Tint,
        });
    }
}
