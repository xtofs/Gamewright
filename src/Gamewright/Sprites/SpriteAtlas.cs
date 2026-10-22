namespace Gamewright.Sprites;

/// <summary>Looks up the sprite for a key, such as a game piece, from the cell the key maps to.</summary>
public sealed class SpriteAtlas<TKey>
    where TKey : notnull
{
    private readonly SpriteSheet sheet;
    private readonly Func<TKey, (int Column, int Row)> locate;
    private readonly Dictionary<TKey, Sprite> sprites = [];

    /// <param name="sheet">The image cut into cells.</param>
    /// <param name="locate">Maps a key to the column and row of its cell; may throw for unknown keys.</param>
    public SpriteAtlas(SpriteSheet sheet, Func<TKey, (int Column, int Row)> locate)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(locate);
        this.sheet = sheet;
        this.locate = locate;
    }

    public Sprite this[TKey key]
    {
        get
        {
            if (!sprites.TryGetValue(key, out var sprite))
            {
                var (column, row) = locate(key);
                sprite = sheet[column, row];
                sprites[key] = sprite;
            }
            return sprite;
        }
    }
}
