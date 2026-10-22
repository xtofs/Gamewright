using Gamewright;

namespace Gamewright.Tests;

public class ImageTests
{
    [Fact]
    public void ExposesItsPath() =>
        Assert.Equal("img/pieces.png", new Image("img/pieces.png").Path);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PathIsRequired(string path) =>
        Assert.Throws<ArgumentException>(() => new Image(path));

    [Fact]
    public void NullPathIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new Image(null!));

    [Fact]
    public void ImagesWithTheSamePathAreEqual()
    {
        Assert.Equal(new Image("a.png"), new Image("a.png"));
        Assert.NotEqual(new Image("a.png"), new Image("b.png"));
    }
}
