namespace HotReloadSpike.Commands;

/// <summary>
/// Identifies a command in a <see cref="Rendering.RenderCommandBuffer"/>. Written as a single
/// byte in front of each command's struct bytes.
/// </summary>
public enum RenderOp : byte
{
    None = 0,
    ClearColor,
    DrawRect,
    DrawCircle,
    DrawLine,
    DrawTriangle,
    DrawSprite,
}
