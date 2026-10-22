namespace Gamewright.Sprites;

/// <summary>A region of an <see cref="Image"/>, in pixels of the image.</summary>
public readonly record struct Sprite(Image Image, Rect Source);
