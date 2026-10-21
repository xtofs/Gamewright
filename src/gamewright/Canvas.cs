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

    /// <summary>Draws a rectangle; the paint says whether it is filled, stroked or both, and in which order.</summary>
    public void DrawRectangle(float x, float y, float width, float height, Paint paint) =>
        builder.AddPath(Rectangle(x, y, width, height), paint);

    public void DrawCircle(float centerX, float centerY, float radius, Paint paint) =>
        builder.AddPath(Circle(centerX, centerY, radius), paint);

    public void DrawLine(float x1, float y1, float x2, float y2, StrokeStyle stroke) =>
        builder.AddPath(path => path.MoveTo(x1, y1).LineTo(x2, y2), stroke);

    public void DrawPath(Action<PathBuilder> buildPath, Paint paint) =>
        builder.AddPath(buildPath, paint);

    /// <summary>Draws SVG path data such as <c>"M0 0 L10 0 L10 10 Z"</c>.</summary>
    public void DrawPath(string svgPath, Paint paint) =>
        builder.AddPath(svgPath, paint);

    /// <summary>Draws text with its vertical middle at <paramref name="y"/>.</summary>
    public void DrawText(
        string text,
        float x,
        float y,
        Color color,
        FontIndex font = FontIndex.Body,
        TextAlignment alignment = TextAlignment.Start) =>
        builder.AddText(text, x, y, color, font, alignment);

    /// <summary>Draws text centred horizontally and vertically in a rectangle.</summary>
    public void DrawTextCentered(
        string text,
        float x,
        float y,
        float width,
        float height,
        Color color,
        FontIndex font = FontIndex.Body)
    {
        if (!(width > 0) || !(height > 0))
            throw new ArgumentOutOfRangeException(nameof(width), "Rectangle dimensions must be positive.");

        DrawText(text, x + width / 2, y + height / 2, color, font, TextAlignment.Center);
    }

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
