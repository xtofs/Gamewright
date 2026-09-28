namespace HotReloadSpike.Tests;

using System.Numerics;
using System.Reflection;
using HotReloadSpike.Commands;
using HotReloadSpike.Rendering;

public class RenderCommandBufferTests
{
    [Fact]
    public void Commands_RoundTripThroughReader()
    {
        var buffer = new RenderCommandBuffer();
        var clear = ClearColorCommand.Create(new Vector4(0.1f, 0.2f, 0.3f, 1));
        var rect = DrawRectCommand.Create(new Vector2(1, 2), new Vector2(3, 4), new Vector4(1, 0, 0, 1), 5, new Vector4(0, 1, 0, 1), 6);
        var line = DrawLineCommand.Create(new Vector2(7, 8), new Vector2(9, 10), 11, new Vector4(0, 0, 1, 1));
        var triangle = DrawTriangleCommand.Create(new Vector2(1, 1), new Vector2(2, 2), new Vector2(3, 3), Vector4.One);
        var circle = DrawCircleCommand.Create(new Vector2(12, 13), 14, Vector4.One);

        buffer.Write(clear);
        buffer.Write(rect);
        buffer.Write(line);
        buffer.Write(triangle);
        buffer.Write(circle);

        Assert.Equal(5, buffer.Count);
        var reader = new RenderCommandReader(buffer.Data);

        // the 1-byte opcodes put every struct after the first at an unaligned offset
        Assert.True(reader.TryReadOp(out var op));
        Assert.Equal(RenderOp.ClearColor, op);
        Assert.Equal(clear, reader.Read<ClearColorCommand>());

        Assert.True(reader.TryReadOp(out op));
        Assert.Equal(RenderOp.DrawRect, op);
        Assert.Equal(rect, reader.Read<DrawRectCommand>());

        Assert.True(reader.TryReadOp(out op));
        Assert.Equal(RenderOp.DrawLine, op);
        Assert.Equal(line, reader.Read<DrawLineCommand>());

        Assert.True(reader.TryReadOp(out op));
        Assert.Equal(RenderOp.DrawTriangle, op);
        Assert.Equal(triangle, reader.Read<DrawTriangleCommand>());

        Assert.True(reader.TryReadOp(out op));
        Assert.Equal(RenderOp.DrawCircle, op);
        Assert.Equal(circle, reader.Read<DrawCircleCommand>());

        Assert.False(reader.TryReadOp(out _));
    }

    [Fact]
    public void Write_GrowsPastInitialCapacity()
    {
        var buffer = new RenderCommandBuffer(capacity: 8);
        for (var i = 0; i < 100; i++)
        {
            buffer.Write(DrawLineCommand.Create(new Vector2(i), new Vector2(i + 1), i, Vector4.One));
        }

        var reader = new RenderCommandReader(buffer.Data);
        for (var i = 0; i < 100; i++)
        {
            Assert.True(reader.TryReadOp(out var op));
            Assert.Equal(RenderOp.DrawLine, op);
            Assert.Equal(i, reader.Read<DrawLineCommand>().Thickness);
        }

        Assert.False(reader.TryReadOp(out _));
    }

    [Fact]
    public void Clear_ResetsDataAndCount()
    {
        var buffer = new RenderCommandBuffer();
        buffer.Write(ClearColorCommand.Create(Vector4.One));

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.True(buffer.Data.IsEmpty);
    }

    [Fact]
    public void Write_WithoutOpCode_Throws()
    {
        var buffer = new RenderCommandBuffer();

        var ex = Assert.Throws<TypeInitializationException>(() => buffer.Write(new Vector2(1, 2)));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void EveryRenderOp_HasExactlyOneCommandStruct()
    {
        var opCodes = typeof(RenderOp).Assembly.GetTypes()
            .Where(t => t.IsValueType)
            .Select(t => t.GetField("OpCode", BindingFlags.Public | BindingFlags.Static))
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(RenderOp))
            .Select(f => (RenderOp)Convert.ToByte(f!.GetRawConstantValue()))
            .Order()
            .ToList();

        var expected = Enum.GetValues<RenderOp>().Where(op => op != RenderOp.None).Order().ToList();
        Assert.Equal(expected, opCodes);
    }
}
