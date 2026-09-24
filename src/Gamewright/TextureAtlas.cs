namespace Gamewright;

using System.Collections;
using System.Numerics;

public sealed class TextureAtlas<TKey> : IReadOnlyDictionary<TKey, TextureRegion>, IDisposable
    where TKey : notnull
{
    private readonly Dictionary<TKey, TextureRegion> _regions;
    private bool _disposed;

    public TextureAtlas(
        Texture2D texture,
        IReadOnlyDictionary<TKey, Rect> pixelRegions)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(pixelRegions);
        if (pixelRegions.Count == 0)
        {
            throw new ArgumentException("The texture atlas must contain at least one region.", nameof(pixelRegions));
        }

        Texture = texture;
        _regions = new Dictionary<TKey, TextureRegion>(pixelRegions.Count);
        foreach (var (key, region) in pixelRegions)
        {
            _regions.Add(key, ToTextureRegion(region, texture.Width, texture.Height));
        }
    }

    public Texture2D Texture { get; }

    public TextureRegion this[TKey key] => _regions[key];

    public IEnumerable<TKey> Keys => _regions.Keys;

    public IEnumerable<TextureRegion> Values => _regions.Values;

    public int Count => _regions.Count;

    public bool ContainsKey(TKey key) => _regions.ContainsKey(key);

    public bool TryGetValue(TKey key, out TextureRegion value) => _regions.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, TextureRegion>> GetEnumerator() => _regions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Texture.Dispose();
        _disposed = true;
    }

    private static TextureRegion ToTextureRegion(Rect region, uint textureWidth, uint textureHeight)
    {
        if (region.X < 0 || region.Y < 0 || region.Width <= 0 || region.Height <= 0
            || region.X + region.Width > textureWidth || region.Y + region.Height > textureHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Atlas regions must fit inside the texture.");
        }

        var minimum = new Vector2(
            region.X / textureWidth,
            region.Y / textureHeight);
        var maximum = new Vector2(
            (region.X + region.Width) / textureWidth,
            (region.Y + region.Height) / textureHeight);
        return new TextureRegion(minimum, maximum);
    }
}
