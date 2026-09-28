namespace HotReloadSpike.Game;

using System.Numerics;
using HotReloadSpike.Commands;
using HotReloadSpike.Rendering;
using HotReloadSpike.Resources;

/// <summary>
/// Named draw methods over a <see cref="RenderCommandBuffer"/>, so game code doesn't build
/// command structs itself. Each call only records a command; nothing is drawn until the render
/// thread replays the buffer. Method names follow Gamewright.Graphics' Canvas.
/// </summary>
public readonly struct Canvas
{
    private readonly RenderCommandBuffer _commands;

    public Canvas(RenderCommandBuffer commands, Vector2 size)
    {
        _commands = commands;
        Size = size;
    }

    /// <summary>Framebuffer size in pixels.</summary>
    public Vector2 Size { get; }

    public void Clear(Vector4 color)
        => _commands.Write(ClearColorCommand.Create(color));

    public void DrawRectangle(Vector2 position, Vector2 size, Vector4 fill, Vector4 stroke = default, float strokeWidth = 0)
        => DrawRoundedRectangle(position, size, 0, fill, stroke, strokeWidth);

    public void DrawRoundedRectangle(
        Vector2 position, Vector2 size, float cornerRadius, Vector4 fill, Vector4 stroke = default, float strokeWidth = 0)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Rectangle dimensions must be positive.");
        }

        _commands.Write(DrawRectCommand.Create(position, size, fill, cornerRadius, stroke, strokeWidth));
    }

    public void DrawCircle(Vector2 center, float radius, Vector4 fill, Vector4 stroke = default, float strokeWidth = 0)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");
        }

        _commands.Write(DrawCircleCommand.Create(center, radius, fill, stroke, strokeWidth));
    }

    public void DrawLine(Vector2 start, Vector2 end, float thickness, Vector4 color)
    {
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        _commands.Write(DrawLineCommand.Create(start, end, thickness, color));
    }

    public void DrawPolyline(ReadOnlySpan<Vector2> points, float thickness, Vector4 color)
    {
        for (var i = 1; i < points.Length; i++)
        {
            DrawLine(points[i - 1], points[i], thickness, color);
        }
    }

    public void DrawTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Vector4 color)
        => _commands.Write(DrawTriangleCommand.Create(v0, v1, v2, color));

    /// <summary>Draws the whole texture.</summary>
    public void DrawSprite(Texture texture, Vector2 position, Vector2 size, Vector4? tint = null)
        => DrawSprite(texture, new Rect(0, 0, texture.Width, texture.Height), position, size, tint);

    /// <summary>Draws a pixel region of the texture, e.g. one cell of a sprite sheet.</summary>
    public void DrawSprite(Texture texture, Rect source, Vector2 position, Vector2 size, Vector4? tint = null)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Sprite dimensions must be positive.");
        }

        _commands.Write(DrawSpriteCommand.Create(texture.Id, position, size, texture.UvOf(source), tint ?? Vector4.One));
    }

    /// <summary>
    /// Lays out the text here on the game thread and writes one sprite per visible glyph.
    /// <paramref name="position"/> is the top-left corner of the first line.
    /// </summary>
    public void DrawText(Font font, string text, Vector2 position, Vector4 color, float scale = 1)
    {
        var pen = new Vector2(position.X, position.Y + font.Ascent * scale);
        foreach (var c in text)
        {
            if (c == '\n')
            {
                pen = new Vector2(position.X, pen.Y + font.LineHeight * scale);
                continue;
            }

            var glyph = font.GetGlyph(c);
            if (glyph.Source.Width > 0 && glyph.Source.Height > 0)
            {
                DrawSprite(font.Atlas, glyph.Source, pen + glyph.Offset * scale, glyph.Source.Size * scale, color);
            }

            pen.X += glyph.Advance * scale;
        }
    }
}
