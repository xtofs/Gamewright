namespace HotReloadSpike.Tests;

using System.Numerics;
using HotReloadSpike.Commands;
using HotReloadSpike.Game;
using HotReloadSpike.Rendering;
using HotReloadSpike.Resources;

public class CanvasTests
{
    [Fact]
    public void DrawMethods_WriteMatchingCommands()
    {
        var buffer = new RenderCommandBuffer();
        var canvas = new Canvas(buffer, new Vector2(800, 600));

        canvas.Clear(Vector4.One);
        canvas.DrawRectangle(new Vector2(1, 2), new Vector2(3, 4), Vector4.One);
        canvas.DrawRoundedRectangle(new Vector2(1, 2), new Vector2(3, 4), 5, Vector4.One);
        canvas.DrawCircle(new Vector2(5, 6), 7, Vector4.One);
        canvas.DrawLine(Vector2.Zero, Vector2.One, 2, Vector4.One);
        canvas.DrawTriangle(Vector2.Zero, Vector2.UnitX, Vector2.UnitY, Vector4.One);

        Assert.Equal(
            [RenderOp.ClearColor, RenderOp.DrawRect, RenderOp.DrawRect, RenderOp.DrawCircle, RenderOp.DrawLine, RenderOp.DrawTriangle],
            ReadOps(buffer));
    }

    [Fact]
    public void DrawRoundedRectangle_PassesArgumentsThrough()
    {
        var buffer = new RenderCommandBuffer();
        var canvas = new Canvas(buffer, Vector2.One);
        var fill = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
        var stroke = new Vector4(0.5f, 0.6f, 0.7f, 0.8f);

        canvas.DrawRoundedRectangle(new Vector2(1, 2), new Vector2(3, 4), 5, fill, stroke, 6);

        var reader = new RenderCommandReader(buffer.Data);
        reader.TryReadOp(out _);
        Assert.Equal(
            DrawRectCommand.Create(new Vector2(1, 2), new Vector2(3, 4), fill, 5, stroke, 6),
            reader.Read<DrawRectCommand>());
    }

    [Fact]
    public void DrawPolyline_WritesOneLinePerSegment()
    {
        var buffer = new RenderCommandBuffer();
        var canvas = new Canvas(buffer, Vector2.One);

        canvas.DrawPolyline([Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY], 2, Vector4.One);

        Assert.Equal([RenderOp.DrawLine, RenderOp.DrawLine, RenderOp.DrawLine], ReadOps(buffer));
    }

    [Fact]
    public void DrawSprite_ConvertsPixelRegionToUv()
    {
        var buffer = new RenderCommandBuffer();
        var canvas = new Canvas(buffer, Vector2.One);
        var texture = new Texture(new TextureId(7), 500, 250);

        canvas.DrawSprite(texture, new Rect(250, 0, 250, 125), new Vector2(10, 20), new Vector2(30, 40));

        var reader = new RenderCommandReader(buffer.Data);
        Assert.True(reader.TryReadOp(out var op));
        Assert.Equal(RenderOp.DrawSprite, op);
        var cmd = reader.Read<DrawSpriteCommand>();
        Assert.Equal(new TextureId(7), cmd.Texture);
        Assert.Equal(new Vector4(10, 20, 30, 40), cmd.Destination);
        Assert.Equal(new Vector4(0.5f, 0, 1, 0.5f), cmd.Uv);
        Assert.Equal(Vector4.One, cmd.Tint);
    }

    [Fact]
    public void DrawText_WritesOneSpritePerVisibleGlyph()
    {
        var atlas = new Texture(new TextureId(1), 64, 64);
        var glyphs = new Dictionary<char, Glyph>
        {
            ['a'] = new Glyph(new Rect(0, 0, 8, 10), new Vector2(1, -10), 9),
            [' '] = new Glyph(new Rect(0, 0, 0, 0), Vector2.Zero, 5),
            ['?'] = new Glyph(new Rect(8, 0, 8, 10), new Vector2(0, -10), 9),
        };
        var font = new Font(atlas, ascent: 12, lineHeight: 16, glyphs);
        var buffer = new RenderCommandBuffer();
        var canvas = new Canvas(buffer, Vector2.One);

        canvas.DrawText(font, "a a\na", new Vector2(100, 200), Vector4.One);

        Assert.Equal([RenderOp.DrawSprite, RenderOp.DrawSprite, RenderOp.DrawSprite], ReadOps(buffer));
        var reader = new RenderCommandReader(buffer.Data);
        var positions = new List<Vector2>();
        while (reader.TryReadOp(out _))
        {
            var d = reader.Read<DrawSpriteCommand>().Destination;
            positions.Add(new Vector2(d.X, d.Y));
        }

        // pen starts on the baseline (y = 200 + ascent); the space advances without a sprite
        Assert.Equal([new Vector2(101, 202), new Vector2(115, 202), new Vector2(101, 218)], positions);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        var canvas = new Canvas(new RenderCommandBuffer(), Vector2.One);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawRectangle(Vector2.Zero, new Vector2(0, 1), Vector4.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawCircle(Vector2.Zero, -1, Vector4.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawLine(Vector2.Zero, Vector2.One, 0, Vector4.One));
    }

    private static List<RenderOp> ReadOps(RenderCommandBuffer buffer)
    {
        var ops = new List<RenderOp>();
        var reader = new RenderCommandReader(buffer.Data);
        while (reader.TryReadOp(out var op))
        {
            ops.Add(op);
            // skip the payload so the next opcode lines up
            _ = op switch
            {
                RenderOp.ClearColor => (object)reader.Read<ClearColorCommand>(),
                RenderOp.DrawRect => reader.Read<DrawRectCommand>(),
                RenderOp.DrawCircle => reader.Read<DrawCircleCommand>(),
                RenderOp.DrawLine => reader.Read<DrawLineCommand>(),
                RenderOp.DrawTriangle => reader.Read<DrawTriangleCommand>(),
                RenderOp.DrawSprite => reader.Read<DrawSpriteCommand>(),
                _ => throw new InvalidOperationException($"Unexpected op {op}"),
            };
        }

        return ops;
    }
}
