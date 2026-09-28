namespace HotReloadSpike.Commands;

using System.Numerics;
using HotReloadSpike.Rendering;
using Silk.NET.OpenGL;

public struct ClearColorCommand
{
    public const RenderOp OpCode = RenderOp.ClearColor;

    public float R, G, B, A;

    public static ClearColorCommand Create(Vector4 color)
        => new ClearColorCommand { R = color.X, G = color.Y, B = color.Z, A = color.W };

    public static void Execute(RenderContext ctx, ref ClearColorCommand cmd)
    {
        // draw anything batched so far first, otherwise the clear would overwrite it
        ctx.Flush();
        ctx.Gl.ClearColor(cmd.R, cmd.G, cmd.B, cmd.A);
        ctx.Gl.Clear(ClearBufferMask.ColorBufferBit);
    }
}
