namespace HotReloadSpike.Resources;

using StbImageSharp;

/// <summary>
/// Game-thread side of resource loading. Decodes images and rasterizes fonts on the CPU, queues
/// the pixels for upload, and returns a handle right away. Results are cached by path, so asking
/// again is cheap. That makes it safe to call from <c>Draw</c>, where a Hot Reload edit can add a
/// new sprite sheet without a restart.
/// </summary>
/// <remarks>Not thread-safe: use it from the game thread only.</remarks>
public sealed class ResourceLoader
{
    private readonly TextureUploadQueue _uploads;
    private readonly Dictionary<string, Texture> _textures = [];
    private readonly Dictionary<(string Path, float PixelHeight), Font> _fonts = [];
    private int _nextId;

    public ResourceLoader(TextureUploadQueue uploads)
        => _uploads = uploads;

    /// <summary>Loads an image file. Relative paths are resolved against the app's output directory.</summary>
    public Texture Texture(string path)
    {
        var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);
        if (_textures.TryGetValue(fullPath, out var texture))
        {
            return texture;
        }

        using var stream = File.OpenRead(fullPath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        texture = Upload(image.Width, image.Height, image.Data);
        _textures[fullPath] = texture;
        return texture;
    }

    public Font Font(string path, float pixelHeight)
    {
        var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);
        if (_fonts.TryGetValue((fullPath, pixelHeight), out var font))
        {
            return font;
        }

        var rasterized = FontRasterizer.Rasterize(File.ReadAllBytes(fullPath), pixelHeight);
        var atlas = Upload(rasterized.AtlasWidth, rasterized.AtlasHeight, rasterized.Rgba);
        font = new Font(atlas, rasterized.Ascent, rasterized.LineHeight, rasterized.Glyphs);
        _fonts[(fullPath, pixelHeight)] = font;
        return font;
    }

    private Texture Upload(int width, int height, byte[] rgba)
    {
        var id = new TextureId(++_nextId);
        _uploads.Enqueue(new TextureUpload(id, width, height, rgba));
        return new Texture(id, width, height);
    }
}
