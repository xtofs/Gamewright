namespace HotReloadSpike.Resources;

using System.Numerics;
using StbTrueTypeSharp;

/// <summary>Rasterized printable ASCII glyphs packed into a white RGBA atlas, alpha = coverage.</summary>
internal sealed record RasterizedFont(
    int AtlasWidth, int AtlasHeight, byte[] Rgba, float Ascent, float LineHeight, Dictionary<char, Glyph> Glyphs);

/// <summary>
/// CPU-only font rasterization with StbTrueType, so it can run on the game thread. Simplified
/// from Gamewright.Graphics' Font: printable ASCII only, packed in rows from the top.
/// </summary>
internal static unsafe class FontRasterizer
{
    private const int AtlasSize = 512;

    public static RasterizedFont Rasterize(byte[] fontData, float pixelHeight)
    {
        fixed (byte* fontPointer = fontData)
        {
            var font = new StbTrueType.stbtt_fontinfo();
            if (StbTrueType.stbtt_InitFont(font, fontPointer, 0) == 0)
            {
                throw new InvalidDataException("The font file could not be initialized.");
            }

            var scale = StbTrueType.stbtt_ScaleForPixelHeight(font, pixelHeight);
            int ascent = 0, descent = 0, lineGap = 0;
            StbTrueType.stbtt_GetFontVMetrics(font, &ascent, &descent, &lineGap);

            var coverage = new byte[AtlasSize * AtlasSize];
            var glyphs = new Dictionary<char, Glyph>();
            var cursorX = 1;
            var cursorY = 1;
            var rowHeight = 0;

            fixed (byte* bitmap = coverage)
            {
                for (var c = ' '; c <= '~'; c++)
                {
                    var glyphIndex = StbTrueType.stbtt_FindGlyphIndex(font, c);
                    int advance = 0, leftSideBearing = 0;
                    StbTrueType.stbtt_GetGlyphHMetrics(font, glyphIndex, &advance, &leftSideBearing);
                    int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
                    StbTrueType.stbtt_GetGlyphBitmapBox(font, glyphIndex, scale, scale, &x0, &y0, &x1, &y1);
                    var width = x1 - x0;
                    var height = y1 - y0;

                    if (cursorX + width + 1 > AtlasSize)
                    {
                        cursorX = 1;
                        cursorY += rowHeight + 1;
                        rowHeight = 0;
                    }

                    if (cursorY + height + 1 > AtlasSize)
                    {
                        throw new InvalidOperationException($"A {AtlasSize}px atlas is too small for a {pixelHeight}px font.");
                    }

                    if (width > 0 && height > 0)
                    {
                        StbTrueType.stbtt_MakeGlyphBitmap(
                            font, bitmap + cursorY * AtlasSize + cursorX, width, height, AtlasSize, scale, scale, glyphIndex);
                    }

                    // y0 is negative for the part above the baseline (stb uses y-down)
                    glyphs[c] = new Glyph(new Rect(cursorX, cursorY, width, height), new Vector2(x0, y0), advance * scale);
                    cursorX += width + 1;
                    rowHeight = Math.Max(rowHeight, height);
                }
            }

            var rgba = new byte[coverage.Length * 4];
            for (var i = 0; i < coverage.Length; i++)
            {
                rgba[i * 4] = 255;
                rgba[i * 4 + 1] = 255;
                rgba[i * 4 + 2] = 255;
                rgba[i * 4 + 3] = coverage[i];
            }

            return new RasterizedFont(
                AtlasSize, AtlasSize, rgba, ascent * scale, (ascent - descent + lineGap) * scale, glyphs);
        }
    }
}
