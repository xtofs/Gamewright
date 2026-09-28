namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;

public struct DrawCircleCommand
{
    public const RenderOp OpCode = RenderOp.DrawCircle;

    public Vector2 Center;
    public float Radius;
    public Vector4 Fill;
    public Vector4 Stroke;
    public float StrokeWidth;

    public static DrawCircleCommand Create(
        Vector2 center, float radius, Vector4 fill, Vector4 stroke = default, float strokeWidth = 0)
        => new DrawCircleCommand
        {
            Center = center,
            Radius = radius,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
        };

    public static void Execute(RenderContext ctx, ref DrawCircleCommand cmd)
    {
        // a circle is a rounded box whose corner radius equals its half extent
        ctx.AddRoundedBox(new RoundedBoxInstance
        {
            Center = cmd.Center,
            HalfExtent = new Vector2(cmd.Radius),
            CornerRadius = cmd.Radius,
            FillColor = cmd.Fill,
            StrokeColor = cmd.Stroke,
            StrokeWidth = cmd.StrokeWidth,
        });
    }
}
