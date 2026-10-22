using Gamewright;
using Gamewright.Sprites;

namespace Gamewright.Tests;

public class SpriteAtlasTests
{
    private enum Piece { Red, Blue }

    private static readonly SpriteSheet Sheet = new(new Image("pieces.png"), 200);

    private static (int Column, int Row) Locate(Piece piece) => piece switch
    {
        Piece.Red => (0, 0),
        Piece.Blue => (2, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(piece))
    };

    [Fact]
    public void KeysMapToTheirCells()
    {
        var atlas = new SpriteAtlas<Piece>(Sheet, Locate);

        Assert.Equal(new Rect(0, 0, 200, 200), atlas[Piece.Red].Source);
        Assert.Equal(new Rect(400, 0, 200, 200), atlas[Piece.Blue].Source);
    }

    [Fact]
    public void EachKeyIsLocatedOnlyOnce()
    {
        var calls = 0;
        var atlas = new SpriteAtlas<Piece>(Sheet, piece =>
        {
            calls++;
            return Locate(piece);
        });

        _ = atlas[Piece.Red];
        _ = atlas[Piece.Red];
        _ = atlas[Piece.Blue];

        Assert.Equal(2, calls);
    }

    [Fact]
    public void UnknownKeysFailInTheMapping() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpriteAtlas<Piece>(Sheet, Locate)[(Piece)9]);

    [Fact]
    public void ArgumentsAreRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new SpriteAtlas<Piece>(null!, Locate));
        Assert.Throws<ArgumentNullException>(() => new SpriteAtlas<Piece>(Sheet, null!));
    }

    [Fact]
    public void DrawSpriteIsDrawImageOfTheSpritesSource()
    {
        var atlas = new SpriteAtlas<Piece>(Sheet, Locate);
        var viaSprite = new Canvas(100, 100);
        viaSprite.DrawSprite(atlas[Piece.Blue], new Rect(10, 20, 30, 40));
        var viaImage = new Canvas(100, 100);
        viaImage.DrawImage(new Image("pieces.png"), new Rect(400, 0, 200, 200), new Rect(10, 20, 30, 40));

        var (a, b) = (viaSprite.Build(), viaImage.Build());
        Assert.Equal(b.Geometry, a.Geometry);
        Assert.Equal(b.Images, a.Images);
        Assert.Equal(b.Commands, a.Commands);
    }
}
