using Gamewright;

namespace Gamewright.Tests;

public class SpriteSheetLayoutTests
{
    [Fact]
    public void ExposesItsGridAndImage()
    {
        var sheet = new SpriteSheetLayout(columns: 7, rows: 4, cellSize: 200, imagePath: "sprites.png");

        Assert.Equal("sprites.png", sheet.ImagePath);
        Assert.Equal(1400, sheet.Width);
        Assert.Equal(800, sheet.Height);
        Assert.Equal(28, sheet.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ImagePathIsRequired(string path) =>
        Assert.Throws<ArgumentException>(() => new SpriteSheetLayout(1, 1, 10, path));

    [Fact]
    public void NullImagePathIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new SpriteSheetLayout(1, 1, 10, null!));

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void GridDimensionsMustBePositive(int columns, int rows, int cellSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpriteSheetLayout(columns, rows, cellSize, "a.png"));

    [Fact]
    public void IndexCountsRowByRowAndChecksBounds()
    {
        var sheet = new SpriteSheetLayout(7, 4, 200, "a.png");

        Assert.Equal(0, sheet.Index(0, 0));
        Assert.Equal(7, sheet.Index(1, 0));
        Assert.Equal(27, sheet.Index(3, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Index(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Index(0, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Index(-1, 0));
    }
}
