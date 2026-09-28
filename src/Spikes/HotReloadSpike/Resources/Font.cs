namespace HotReloadSpike.Resources;

using System.Numerics;

/// <summary>
/// Where a glyph sits in the font atlas and how to place it relative to the pen position on the
/// baseline. All values are in pixels at the font's rasterized size.
/// </summary>
public readonly record struct Glyph(Rect Source, Vector2 Offset, float Advance);

/// <summary>
/// Game-side font: glyph metrics for layout plus the atlas texture handle. Text is laid out on
/// the game thread and drawn as one sprite per glyph, so the render thread never sees strings.
/// </summary>
public sealed class Font
{
    private const char Fallback = '?';

    private readonly Dictionary<char, Glyph> _glyphs;

    internal Font(Texture atlas, float ascent, float lineHeight, Dictionary<char, Glyph> glyphs)
    {
        Atlas = atlas;
        Ascent = ascent;
        LineHeight = lineHeight;
        _glyphs = glyphs;
    }

    public Texture Atlas { get; }

    /// <summary>Distance from the top of a line to its baseline.</summary>
    public float Ascent { get; }

    public float LineHeight { get; }

    public Glyph GetGlyph(char c)
        => _glyphs.TryGetValue(c, out var glyph) ? glyph : _glyphs[Fallback];

    public Vector2 Measure(string text, float scale = 1)
    {
        var width = 0f;
        var lineWidth = 0f;
        var lines = 1;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                width = MathF.Max(width, lineWidth);
                lineWidth = 0;
                lines++;
                continue;
            }

            lineWidth += GetGlyph(c).Advance;
        }

        return new Vector2(MathF.Max(width, lineWidth), lines * LineHeight) * scale;
    }
}
