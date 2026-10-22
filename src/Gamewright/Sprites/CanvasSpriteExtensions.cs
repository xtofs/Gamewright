namespace Gamewright.Sprites;

public static class CanvasSpriteExtensions
{
    /// <summary>Draws a sprite scaled into <paramref name="destination"/>; a shorthand for <see cref="Canvas.DrawImage"/>.</summary>
    public static void DrawSprite(this Canvas canvas, Sprite sprite, Rect destination)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        canvas.DrawImage(sprite.Image, sprite.Source, destination);
    }
}
