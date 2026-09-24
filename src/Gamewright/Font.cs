namespace Gamewright;

using System.Numerics;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Silk.NET.OpenGL;
using StbTrueTypeSharp;

public readonly record struct FontGlyph(
    TextureRegion Region,
    Vector2 Size,
    Vector2 Bearing,
    float Advance);

public readonly record struct PositionedGlyph(
    TextureRegion Region,
    Rect Destination);

public sealed class Font : IDisposable
{
    private readonly GL? _gl;
    private readonly Dictionary<Rune, FontGlyph> _glyphs;
    private readonly Rune? _fallback;
    private readonly bool _ownsTexture;
    private bool _disposed;
    private readonly MemoryCache _layoutCache = new(new MemoryCacheOptions { SizeLimit = 256 });
    private CancellationTokenSource _cacheGen = new();

    // Non-null when this Font supports on-demand glyph rasterisation.
    private readonly AtlasState? _atlas;

    internal Font(
        GL? gl,
        Texture2D texture,
        float lineHeight,
        Dictionary<Rune, FontGlyph> glyphs,
        Rune? fallback = null,
        bool ownsTexture = false,
        AtlasState? atlas = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(glyphs);
        if (lineHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be positive.");
        }

        if (glyphs.Count == 0)
        {
            throw new ArgumentException("The atlas must contain at least one glyph.", nameof(glyphs));
        }

        foreach (var (rune, glyph) in glyphs)
        {
            ValidateGlyph(rune, glyph);
        }

        if (fallback is not null && !glyphs.ContainsKey(fallback.Value))
        {
            throw new ArgumentException("The fallback rune must have a glyph entry.", nameof(fallback));
        }

        _gl = gl;
        Texture = texture;
        LineHeight = lineHeight;
        _glyphs = glyphs;
        _fallback = fallback;
        _ownsTexture = ownsTexture;
        _atlas = atlas;
    }

    internal static unsafe Font FromFontFile(
        GL gl,
        ReadOnlySpan<byte> fontData,
        float pixelHeight,
        Rune? fallback = null,
        uint atlasWidth = 512,
        uint atlasHeight = 512)
    {
        ArgumentNullException.ThrowIfNull(gl);
        if (fontData.IsEmpty)
        {
            throw new ArgumentException("Font data cannot be empty.", nameof(fontData));
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Font size must be positive.");
        }

        if (atlasWidth == 0 || atlasHeight == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atlasWidth), "Atlas dimensions must be positive.");
        }

        // Keep a copy for on-demand rasterisation of glyphs outside the pre-filled range.
        var fontDataCopy = fontData.ToArray();

        fixed (byte* fontPointer = fontDataCopy)
        {
            var font = new StbTrueType.stbtt_fontinfo();
            if (StbTrueType.stbtt_InitFont(font, fontPointer, 0) == 0)
            {
                throw new InvalidDataException("The font file could not be initialized.");
            }

            var scale = StbTrueType.stbtt_ScaleForPixelHeight(font, pixelHeight);
            int ascent = 0, descent = 0, lineGap = 0;
            StbTrueType.stbtt_GetFontVMetrics(font, &ascent, &descent, &lineGap);
            var lineHeight = (ascent - descent + lineGap) * scale;

            // Pre-fill Latin-1: U+0020 (space) through U+00FF.
            var rasterGlyphs = new List<RasterGlyph>(0x100 - 0x20);
            for (var i = 0x20; i < 0x100; i++)
            {
                var rune = new Rune(i);
                var glyphIndex = StbTrueType.stbtt_FindGlyphIndex(font, rune.Value);
                int advance = 0, lsb = 0;
                StbTrueType.stbtt_GetGlyphHMetrics(font, glyphIndex, &advance, &lsb);
                int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
                StbTrueType.stbtt_GetGlyphBitmapBox(font, glyphIndex, scale, scale, &x0, &y0, &x1, &y1);
                rasterGlyphs.Add(new RasterGlyph(rune, glyphIndex, x0, y0, x1, y1, advance * scale, ascent * scale));
            }

            var bitmap = new byte[checked((int)(atlasWidth * atlasHeight))];
            var glyphs = new Dictionary<Rune, FontGlyph>();
            var cursorX = 1;
            var cursorY = 1;
            var rowHeight = 0;

            fixed (byte* bitmapPointer = bitmap)
            {
                foreach (var g in rasterGlyphs)
                {
                    var width = g.X1 - g.X0;
                    var height = g.Y1 - g.Y0;
                    if (cursorX + width + 1 > atlasWidth)
                    {
                        cursorX = 1;
                        cursorY += rowHeight + 1;
                        rowHeight = 0;
                    }

                    if (cursorY + height + 1 > atlasHeight)
                    {
                        throw new InvalidOperationException("The font atlas is too small for the Latin-1 pre-fill.");
                    }

                    var bottom = (int)atlasHeight - cursorY - height;
                    if (width > 0 && height > 0)
                    {
                        StbTrueType.stbtt_MakeGlyphBitmap(
                            font,
                            bitmapPointer + bottom * (int)atlasWidth + cursorX,
                            width,
                            height,
                            (int)atlasWidth,
                            scale,
                            scale,
                            g.GlyphIndex);
                    }

                    glyphs[g.Rune] = new FontGlyph(
                        new TextureRegion(
                            new Vector2((float)cursorX / atlasWidth, (float)bottom / atlasHeight),
                            new Vector2((float)(cursorX + width) / atlasWidth, (float)(bottom + height) / atlasHeight)),
                        new Vector2(width, height),
                        new Vector2(g.X0, g.Ascent + g.Y0),
                        g.Advance);
                    cursorX += width + 1;
                    rowHeight = Math.Max(rowHeight, height);
                }
            }

            var rgba = new byte[bitmap.Length * 4];
            for (var index = 0; index < bitmap.Length; index++)
            {
                var rgbaIndex = index * 4;
                rgba[rgbaIndex] = 255;
                rgba[rgbaIndex + 1] = 255;
                rgba[rgbaIndex + 2] = 255;
                rgba[rgbaIndex + 3] = bitmap[index];
            }

            var atlas = new AtlasState(fontDataCopy, scale, ascent * scale, atlasWidth, atlasHeight, cursorX, cursorY, rowHeight);
            var effectiveFallback = fallback ?? new Rune('?');

            return new Font(
                gl,
                new Texture2D(gl, atlasWidth, atlasHeight, rgba),
                lineHeight,
                glyphs,
                effectiveFallback,
                ownsTexture: true,
                atlas: atlas);
        }
    }

    public static Font FromFile(GL gl, string path, float pixelHeight)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The font file was not found.", path);
        }

        return FromFontFile(
            gl,
            File.ReadAllBytes(path),
            pixelHeight,
            fallback: new Rune('?'),
            atlasWidth: 1024,
            atlasHeight: 512);
    }

    internal Texture2D Texture { get; }

    internal float LineHeight { get; }

    /// <summary>
    /// Returns the bounding size of <paramref name="text"/> at the given scale.
    /// </summary>
    public Vector2 Measure(string text, float scale = 1f)
    {
        var glyphs = LayoutAtOrigin(text, scale);
        if (glyphs.Count == 0)
        {
            return Vector2.Zero;
        }

        var max = Vector2.Zero;
        foreach (var g in glyphs)
        {
            if (g.Destination.Right > max.X)
            {
                max.X = g.Destination.Right;
            }

            if (g.Destination.Bottom > max.Y)
            {
                max.Y = g.Destination.Bottom;
            }
        }
        return max;
    }

    /// <summary>
    /// Returns the layout of <paramref name="text"/> at <see cref="Vector2.Zero"/> and the given scale,
    /// from a cache. Results are evicted after a new glyph is rasterised or after the cache fills.
    /// </summary>
    internal IReadOnlyList<PositionedGlyph> LayoutAtOrigin(string text, float scale = 1f)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Text scale must be positive.");
        }

        // Ensure all runes in the text are in the atlas before we cache the layout.
        foreach (var rune in text.EnumerateRunes())
        {
            EnsureGlyph(rune);
        }

        return _layoutCache.GetOrCreate(
            (text, scale),
            entry =>
            {
                entry.Size = 1;
                entry.AddExpirationToken(new CancellationChangeToken(_cacheGen.Token));
                return FontLayout.Layout(text, Vector2.Zero, scale, LineHeight, _glyphs, _fallback);
            })!;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsTexture)
        {
            Texture.Dispose();
        }

        _layoutCache.Dispose();
        _cacheGen.Dispose();
        _disposed = true;
    }

    private unsafe void EnsureGlyph(Rune rune)
    {
        if (_glyphs.ContainsKey(rune) || _atlas is null || _gl is null)
        {
            return;
        }

        fixed (byte* fontPointer = _atlas.FontData)
        {
            var font = new StbTrueType.stbtt_fontinfo();
            if (StbTrueType.stbtt_InitFont(font, fontPointer, 0) == 0)
            {
                return;
            }

            var glyphIndex = StbTrueType.stbtt_FindGlyphIndex(font, rune.Value);
            if (glyphIndex == 0)
            {
                // Font has no glyph for this character — alias to fallback so we don't retry.
                if (_fallback is { } fb && _glyphs.TryGetValue(fb, out var fbGlyph))
                {
                    _glyphs[rune] = fbGlyph;
                }
                return;
            }

            int advance = 0, lsb = 0;
            StbTrueType.stbtt_GetGlyphHMetrics(font, glyphIndex, &advance, &lsb);
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            StbTrueType.stbtt_GetGlyphBitmapBox(font, glyphIndex, _atlas.Scale, _atlas.Scale, &x0, &y0, &x1, &y1);

            var width = x1 - x0;
            var height = y1 - y0;

            if (_atlas.CursorX + width + 1 > _atlas.Width)
            {
                _atlas.CursorX = 1;
                _atlas.CursorY += _atlas.RowHeight + 1;
                _atlas.RowHeight = 0;
            }

            if (_atlas.CursorY + height + 1 > _atlas.Height)
            {
                throw new InvalidOperationException($"Font atlas is full; cannot rasterise U+{rune.Value:X4}.");
            }

            var bottom = (int)_atlas.Height - _atlas.CursorY - height;

            if (width > 0 && height > 0)
            {
                var glyphBitmap = new byte[width * height];
                fixed (byte* bitmapPtr = glyphBitmap)
                {
                    StbTrueType.stbtt_MakeGlyphBitmap(font, bitmapPtr, width, height, width, _atlas.Scale, _atlas.Scale, glyphIndex);
                }

                var rgba = new byte[width * height * 4];
                for (var i = 0; i < glyphBitmap.Length; i++)
                {
                    var j = i * 4;
                    rgba[j] = rgba[j + 1] = rgba[j + 2] = 255;
                    rgba[j + 3] = glyphBitmap[i];
                }

                Texture.Bind();
                _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                fixed (byte* rgbaPtr = rgba)
                {
                    _gl.TexSubImage2D(
                        TextureTarget.Texture2D, 0,
                        _atlas.CursorX, bottom,
                        (uint)width, (uint)height,
                        PixelFormat.Rgba, PixelType.UnsignedByte,
                        rgbaPtr);
                }
            }

            _glyphs[rune] = new FontGlyph(
                new TextureRegion(
                    new Vector2((float)_atlas.CursorX / _atlas.Width, (float)bottom / _atlas.Height),
                    new Vector2((float)(_atlas.CursorX + width) / _atlas.Width, (float)(bottom + height) / _atlas.Height)),
                new Vector2(width, height),
                new Vector2(x0, _atlas.Ascent + y0),
                advance * _atlas.Scale);

            _atlas.CursorX += width + 1;
            _atlas.RowHeight = Math.Max(_atlas.RowHeight, height);
        }

        // Invalidate cached layouts; they may have used the fallback for this rune.
        var old = _cacheGen;
        _cacheGen = new CancellationTokenSource();
        old.Cancel();
        old.Dispose();
    }

    private static void ValidateGlyph(Rune rune, FontGlyph glyph)
    {
        if (glyph.Size.X < 0 || glyph.Size.Y < 0)
        {
            throw new ArgumentException($"Glyph '{rune}' cannot have negative dimensions.");
        }

        if (glyph.Advance < 0)
        {
            throw new ArgumentException($"Glyph '{rune}' cannot have a negative advance.");
        }
    }

    private readonly record struct RasterGlyph(
        Rune Rune,
        int GlyphIndex,
        int X0,
        int Y0,
        int X1,
        int Y1,
        float Advance,
        float Ascent);

    internal sealed class AtlasState(
        byte[] fontData,
        float scale,
        float ascent,
        uint width,
        uint height,
        int cursorX,
        int cursorY,
        int rowHeight)
    {
        public readonly byte[] FontData = fontData;
        public readonly float Scale = scale;
        public readonly float Ascent = ascent;
        public readonly uint Width = width;
        public readonly uint Height = height;
        public int CursorX = cursorX;
        public int CursorY = cursorY;
        public int RowHeight = rowHeight;
    }
}

internal static class FontLayout
{
    public static IReadOnlyList<PositionedGlyph> Layout(
        string text,
        Vector2 origin,
        float scale,
        float lineHeight,
        IReadOnlyDictionary<Rune, FontGlyph> glyphs,
        Rune? fallback)
    {
        var positioned = new List<PositionedGlyph>(text.Length);
        var pen = origin;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\r')
            {
                continue;
            }

            if (rune.Value == '\n')
            {
                pen = new Vector2(origin.X, pen.Y + lineHeight * scale);
                continue;
            }

            var glyph = ResolveGlyph(rune, glyphs, fallback);
            if (rune.Value == '\t')
            {
                pen.X += glyph.Advance * 4 * scale;
                continue;
            }

            if (glyph.Size.X == 0 || glyph.Size.Y == 0)
            {
                pen.X += glyph.Advance * scale;
                continue;
            }

            positioned.Add(new PositionedGlyph(
                glyph.Region,
                new Rect(
                    pen.X + glyph.Bearing.X * scale,
                    pen.Y + glyph.Bearing.Y * scale,
                    glyph.Size.X * scale,
                    glyph.Size.Y * scale)));
            pen.X += glyph.Advance * scale;
        }

        return positioned;
    }

    private static FontGlyph ResolveGlyph(
        Rune rune,
        IReadOnlyDictionary<Rune, FontGlyph> glyphs,
        Rune? fallback)
    {
        if (glyphs.TryGetValue(rune, out var glyph))
        {
            return glyph;
        }

        if (fallback is { } fallbackRune && glyphs.TryGetValue(fallbackRune, out glyph))
        {
            return glyph;
        }

        throw new InvalidOperationException($"The font atlas has no glyph for '{rune}'.");
    }
}
