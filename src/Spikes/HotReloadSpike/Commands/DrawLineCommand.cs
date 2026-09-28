namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;

public struct DrawLineCommand
{
    public const RenderOp OpCode = RenderOp.DrawLine;

    public Vector2 Start, End;
    public float Thickness;
    public Vector4 Color;

    public static DrawLineCommand Create(Vector2 start, Vector2 end, float thickness, Vector4 color)
        => new DrawLineCommand { Start = start, End = end, Thickness = thickness, Color = color };

    public static void Execute(RenderContext ctx, ref DrawLineCommand cmd)
    {
        ctx.AddCapsule(new CapsuleInstance
        {
            Start = cmd.Start,
            End = cmd.End,
            Thickness = cmd.Thickness,
            Color = cmd.Color,
        });
    }
}
