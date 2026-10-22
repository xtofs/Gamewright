using Gamewright;

namespace Gamewright.Tests;

public class LetterboxTests
{
    [Fact]
    public void ExactFitHasNoOffset()
    {
        var box = Letterbox.Fit(640, 620, 1280, 1240);

        Assert.Equal(2f, box.Scale);
        Assert.Equal(0f, box.OffsetX);
        Assert.Equal(0f, box.OffsetY);
    }

    [Fact]
    public void WideBoxIsLimitedByHeightAndCenteredHorizontally()
    {
        var box = Letterbox.Fit(100, 100, 400, 200);

        Assert.Equal(2f, box.Scale);
        Assert.Equal(100f, box.OffsetX);
        Assert.Equal(0f, box.OffsetY);
    }

    [Fact]
    public void TallBoxIsLimitedByWidthAndCenteredVertically()
    {
        var box = Letterbox.Fit(100, 100, 200, 400);

        Assert.Equal(2f, box.Scale);
        Assert.Equal(0f, box.OffsetX);
        Assert.Equal(100f, box.OffsetY);
    }

    [Fact]
    public void ConvertsBetweenBoxAndFrameCoordinates()
    {
        var box = Letterbox.Fit(100, 100, 400, 200);

        Assert.Equal((100f, 0f), box.FromFrame(0, 0));
        Assert.Equal((300f, 200f), box.FromFrame(100, 100));
        Assert.Equal((50f, 50f), box.ToFrame(200, 100));
        Assert.Equal((0f, 0f), box.ToFrame(100, 0));
    }

    [Fact]
    public void ToFrameAndFromFrameAreInverse()
    {
        var box = Letterbox.Fit(640, 620, 913, 477);

        var (x, y) = box.FromFrame(123.5f, 456.25f);
        var (frameX, frameY) = box.ToFrame(x, y);

        Assert.Equal(123.5f, frameX, 3);
        Assert.Equal(456.25f, frameY, 3);
    }

    [Fact]
    public void PointsInTheBarsMapOutsideTheFrame()
    {
        var box = Letterbox.Fit(100, 100, 400, 200);

        var (x, _) = box.ToFrame(10, 100);

        Assert.True(x < 0);
    }

    [Theory]
    [InlineData(0f, 100f)]
    [InlineData(100f, 0f)]
    [InlineData(-1f, -1f)]
    public void EmptyBoxIsEmptyAndRefusesConversion(float width, float height)
    {
        var box = Letterbox.Fit(100, 100, width, height);

        Assert.True(box.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => box.ToFrame(1, 1));
        Assert.Throws<InvalidOperationException>(() => box.FromFrame(1, 1));
    }

    [Fact]
    public void FrameSizeMustBePositive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Letterbox.Fit(0, 100, 10, 10));
}
