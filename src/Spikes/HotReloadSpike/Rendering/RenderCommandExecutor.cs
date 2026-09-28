namespace HotReloadSpike.Rendering;

using HotReloadSpike.Commands;

/// <summary>
/// Replays a recorded command stream against a <see cref="RenderContext"/>. Runs on the render
/// thread; this is the only place that maps opcodes back to command structs.
/// </summary>
public static class RenderCommandExecutor
{
    public static void Execute(ReadOnlySpan<byte> data, RenderContext ctx)
    {
        var reader = new RenderCommandReader(data);
        while (reader.TryReadOp(out var op))
        {
            switch (op)
            {
                case RenderOp.ClearColor:
                {
                    var cmd = reader.Read<ClearColorCommand>();
                    ClearColorCommand.Execute(ctx, ref cmd);
                    break;
                }
                case RenderOp.DrawRect:
                {
                    var cmd = reader.Read<DrawRectCommand>();
                    DrawRectCommand.Execute(ctx, ref cmd);
                    break;
                }
                case RenderOp.DrawCircle:
                {
                    var cmd = reader.Read<DrawCircleCommand>();
                    DrawCircleCommand.Execute(ctx, ref cmd);
                    break;
                }
                case RenderOp.DrawLine:
                {
                    var cmd = reader.Read<DrawLineCommand>();
                    DrawLineCommand.Execute(ctx, ref cmd);
                    break;
                }
                case RenderOp.DrawTriangle:
                {
                    var cmd = reader.Read<DrawTriangleCommand>();
                    DrawTriangleCommand.Execute(ctx, ref cmd);
                    break;
                }
                case RenderOp.DrawSprite:
                {
                    var cmd = reader.Read<DrawSpriteCommand>();
                    DrawSpriteCommand.Execute(ctx, ref cmd);
                    break;
                }
                default:
                    // the rest of the stream can't be decoded without knowing this command's size
                    throw new InvalidOperationException($"Unknown render op {(byte)op}.");
            }
        }
    }
}
