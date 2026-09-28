namespace HotReloadSpike.Tests;

using HotReloadSpike.Resources;

public class ResourceLoaderTests
{
    private const string AtlasPath = "Assets/pieces_atlas.png";
    private const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    [Fact]
    public void Texture_DecodesAndQueuesOneUpload()
    {
        var uploads = new TextureUploadQueue();
        var loader = new ResourceLoader(uploads);

        var texture = loader.Texture(AtlasPath);

        Assert.True(uploads.TryDequeue(out var upload));
        Assert.Equal(texture.Id, upload.Id);
        Assert.Equal(texture.Width * texture.Height * 4, upload.Rgba.Length);
        Assert.False(uploads.TryDequeue(out _));
    }

    [Fact]
    public void Texture_SamePathTwice_IsCachedAndUploadedOnce()
    {
        var uploads = new TextureUploadQueue();
        var loader = new ResourceLoader(uploads);

        var first = loader.Texture(AtlasPath);
        var second = loader.Texture(Path.Combine(AppContext.BaseDirectory, AtlasPath));

        Assert.Same(first, second);
        Assert.True(uploads.TryDequeue(out _));
        Assert.False(uploads.TryDequeue(out _));
    }

    [Fact]
    public void Font_RasterizesGlyphsAndQueuesAtlas()
    {
        if (!File.Exists(FontPath))
        {
            return; // macOS system font
        }

        var uploads = new TextureUploadQueue();
        var loader = new ResourceLoader(uploads);

        var font = loader.Font(FontPath, 32);

        Assert.True(uploads.TryDequeue(out var upload));
        Assert.Equal(font.Atlas.Id, upload.Id);
        Assert.Same(font, loader.Font(FontPath, 32));
        Assert.NotEqual(font.Atlas.Id, loader.Font(FontPath, 16).Atlas.Id);

        var a = font.GetGlyph('A');
        Assert.True(a.Source.Width > 0 && a.Source.Height > 0);
        Assert.True(a.Offset.Y < 0, "glyphs sit above the baseline");
        Assert.Equal(0, font.GetGlyph(' ').Source.Width);
        Assert.Equal(font.GetGlyph('?'), font.GetGlyph('€'));
        Assert.Equal(2 * font.LineHeight, font.Measure("A\nB").Y);
    }
}
