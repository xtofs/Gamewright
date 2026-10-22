using Gamewright;
using Gamewright.Sprites;

namespace Gamewright.Tests;

public class SpriteSheetTests
{
    private static readonly Image Pieces = new("pieces.png");

    [Fact]
    public void CellsAreSlicedFromTheGridByColumnAndRow()
    {
        var sheet = new SpriteSheet(Pieces, cellSize: 200);

        var sprite = sheet[2, 1];

        Assert.Equal(Pieces, sprite.Image);
        Assert.Equal(new Rect(400, 200, 200, 200), sprite.Source);
    }

    [Fact]
    public void CellsMayBeRectangular()
    {
        var sheet = new SpriteSheet(Pieces, cellWidth: 32, cellHeight: 48);

        Assert.Equal(new Rect(64, 96, 32, 48), sheet[2, 2].Source);
    }

    [Fact]
    public void FirstCellIsAtTheOrigin() =>
        Assert.Equal(new Rect(0, 0, 10, 10), new SpriteSheet(Pieces, 10)[0, 0].Source);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 5)]
    public void CellSizeMustBePositive(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpriteSheet(Pieces, width, height));

    [Fact]
    public void NegativeColumnsAndRowsAreRejected()
    {
        var sheet = new SpriteSheet(Pieces, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => sheet[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet[0, -1]);
    }
}
