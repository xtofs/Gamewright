namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;

public struct DrawRectCommand
{
    public const RenderOp OpCode = RenderOp.DrawRect;

    public Vector2 Position, Size;
    public float CornerRadius;
    public Vector4 Fill;
    public Vector4 Stroke;
    public float StrokeWidth;

    public static DrawRectCommand Create(
        Vector2 position, Vector2 size, Vector4 fill,
        float cornerRadius = 0, Vector4 stroke = default, float strokeWidth = 0)
        => new DrawRectCommand
        {
            Position = position,
            Size = size,
            CornerRadius = cornerRadius,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
        };

    public static void Execute(RenderContext ctx, ref DrawRectCommand cmd)
    {
        var halfExtent = cmd.Size * 0.5f;
        ctx.AddRoundedBox(new RoundedBoxInstance
        {
            Center = cmd.Position + halfExtent,
            HalfExtent = halfExtent,
            CornerRadius = MathF.Min(cmd.CornerRadius, MathF.Min(halfExtent.X, halfExtent.Y)),
            FillColor = cmd.Fill,
            StrokeColor = cmd.Stroke,
            StrokeWidth = cmd.StrokeWidth,
        });
    }
}
