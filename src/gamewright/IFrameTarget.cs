namespace Gamewright;

/// <summary>
/// A drawing machine that <see cref="FramePlayer"/> drives: a window, an image, a canvas. It sees
/// plain drawing operations in a normalised order and nothing of how a <see cref="Frame"/> is encoded.
/// </summary>
/// <remarks>
/// A path is <see cref="BeginPath"/>, then one or more subpaths, then <see cref="FillPath"/> and/or
/// <see cref="StrokePath"/> in paint order. Each subpath starts with <see cref="MoveTo"/> and may end
/// with <see cref="ClosePath"/>; arcs always have distinct end points and non-zero radii.
/// </remarks>
public interface IFrameTarget
{
    void BeginPath();

    void MoveTo(float x, float y);

    void LineTo(float x, float y);

    void QuadTo(float controlX, float controlY, float x, float y);

    void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y);

    /// <summary>An SVG elliptical arc from the current point; rotation is in degrees.</summary>
    void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, float x, float y);

    void ClosePath();

    /// <summary>Fills the path built since <see cref="BeginPath"/>; colour is packed as in <see cref="Color.Rgba"/>.</summary>
    void FillPath(uint rgba);

    /// <summary>Strokes the path built since <see cref="BeginPath"/>.</summary>
    void StrokePath(uint rgba, float width);

    /// <summary>Draws text with its vertical middle at <paramref name="y"/>, aligned to <paramref name="x"/>.</summary>
    void DrawText(string text, float x, float y, TextStyle style, TextAlignment alignment);

    /// <summary>Draws one cell of the app's sprite sheet scaled into the rectangle.</summary>
    void DrawSprite(int spriteIndex, float x, float y, float width, float height);
}
