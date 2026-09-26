namespace Gamewright.Graphics;

using Silk.NET.OpenGL;

public sealed class GraphicsDevice
{
    private readonly GL _gl;

    internal GraphicsDevice(GL gl) => _gl = gl;

    public Font LoadFont(string path, float pixelHeight)
        => Font.FromFile(_gl, path, pixelHeight);

    public Texture2D LoadTexture(string path)
        => Texture2D.FromFile(_gl, path);

    public TextureAtlas<TKey> LoadAtlas<TKey>(string path, IReadOnlyDictionary<TKey, Rect> regions)
        where TKey : notnull
    {
        var texture = Texture2D.FromFile(_gl, path);
        return new TextureAtlas<TKey>(texture, regions);
    }
}
