namespace Gamewright;

/// <summary>
/// An immediate-mode drawing surface in frame coordinates (<see cref="Width"/> x <see cref="Height"/>,
/// origin top left). Drawing calls are recorded in order, later ones on top, and turned into a
/// <see cref="Frame"/> for the renderer.
/// </summary>
public sealed class Canvas
{
    private readonly FrameBuilder builder = new();

    public Canvas(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public void FillRectangle(float x, float y, float width, float height, Color color) =>
        builder.AddPath(Rectangle(x, y, width, height), fillColor: color);

    public void StrokeRectangle(float x, float y, float width, float height, StrokeStyle stroke) =>
        builder.AddPath(Rectangle(x, y, width, height), strokeStyle: stroke);

    public void FillCircle(float centerX, float centerY, float radius, Color color) =>
        builder.AddPath(Circle(centerX, centerY, radius), fillColor: color);

    public void StrokeCircle(float centerX, float centerY, float radius, StrokeStyle stroke) =>
        builder.AddPath(Circle(centerX, centerY, radius), strokeStyle: stroke);

    public void DrawLine(float x1, float y1, float x2, float y2, StrokeStyle stroke) =>
        builder.AddPath(path => path.MoveTo(x1, y1).LineTo(x2, y2), strokeStyle: stroke);

    public void FillPath(Action<PathBuilder> buildPath, Color color) =>
        builder.AddPath(buildPath, fillColor: color);

    public void StrokePath(Action<PathBuilder> buildPath, StrokeStyle stroke) =>
        builder.AddPath(buildPath, strokeStyle: stroke);

    /// <summary>Fills SVG path data such as <c>"M0 0 L10 0 L10 10 Z"</c>.</summary>
    public void FillPath(string svgPath, Color color) =>
        builder.AddPath(svgPath, fillColor: color);

    /// <summary>Strokes SVG path data such as <c>"M0 0 L10 0 L10 10"</c>.</summary>
    public void StrokePath(string svgPath, StrokeStyle stroke) =>
        builder.AddPath(svgPath, strokeStyle: stroke);

    /// <summary>Draws text with its vertical middle at <paramref name="y"/>.</summary>
    public void DrawText(
        string text,
        float x,
        float y,
        Color color,
        FontIndex font = FontIndex.Body,
        TextAlignment alignment = TextAlignment.Start) =>
        builder.AddText(text, x, y, color, font, alignment);

    /// <summary>Draws one cell of the sprite sheet (see <see cref="SpriteSheet"/>) scaled into the rectangle.</summary>
    public void DrawSprite(int spriteIndex, float x, float y, float width, float height) =>
        builder.AddSprite(spriteIndex, x, y, width, height);

    public Frame Build() => builder.Build();

    private static Action<PathBuilder> Rectangle(float x, float y, float width, float height)
    {
        if (!(width > 0) || !(height > 0))
            throw new ArgumentOutOfRangeException(nameof(width), "Rectangle dimensions must be positive.");

        return path => path.MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height)
            .LineTo(x, y + height).Close();
    }

    private static Action<PathBuilder> Circle(float centerX, float centerY, float radius)
    {
        if (!(radius > 0))
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");

        return path => path.MoveTo(centerX - radius, centerY)
            .ArcTo(radius, radius, 0, false, true, centerX + radius, centerY)
            .ArcTo(radius, radius, 0, false, true, centerX - radius, centerY)
            .Close();
    }
}
