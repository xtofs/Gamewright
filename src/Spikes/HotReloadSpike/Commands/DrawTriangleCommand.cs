namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;

public struct DrawTriangleCommand
{
    public const RenderOp OpCode = RenderOp.DrawTriangle;

    public Vector2 V0, V1, V2;
    public Vector4 Color;

    public static DrawTriangleCommand Create(Vector2 v0, Vector2 v1, Vector2 v2, Vector4 color)
        => new DrawTriangleCommand { V0 = v0, V1 = v1, V2 = v2, Color = color };

    public static void Execute(RenderContext ctx, ref DrawTriangleCommand cmd)
    {
        ctx.AddTriangle(new TriangleInstance
        {
            V0 = cmd.V0,
            V1 = cmd.V1,
            V2 = cmd.V2,
            Color = cmd.Color,
        });
    }
}
