using Gamewright;

namespace Gamewright.Tests;

public class FrameBuilderTests
{
    private static readonly Color Red = new(255, 0, 0);

    [Fact]
    public void PathNeedsFillOrStroke() =>
        Assert.Throws<ArgumentException>(() => new FrameBuilder().AddPath("M0 0 L1 1"));

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void StrokeWidthMustBePositiveAndFinite(float width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FrameBuilder().AddPath(path => path.MoveTo(0, 0).LineTo(1, 1), strokeStyle: new StrokeStyle(Red, width)));

    [Fact]
    public void PathCommandPointsAtItsData()
    {
        var frame = new FrameBuilder()
            .AddPath(path => path.MoveTo(0, 0).LineTo(1, 1).LineTo(2, 0).Close(), fillColor: Red)
            .AddPath("M5 5 L6 6", strokeStyle: new StrokeStyle(Red, 2))
            .Build();

        var second = frame.Commands[1];
        Assert.Equal(DrawCommandType.Path, second.Type);
        Assert.Equal(6, second.GeometryOffset);
        Assert.Equal(4, second.GeometryLength);
        Assert.Equal(4, second.PathDataOffset);
        Assert.Equal(2, second.PathDataLength);
        Assert.Equal((ushort)DrawCommandFlags.Stroke, second.Flags);
        Assert.Equal((ushort)(DrawCommandFlags.Fill | DrawCommandFlags.Closed), frame.Commands[0].Flags);
        Assert.Equal(2f, frame.Styles[second.StyleIndex].StrokeWidth);
    }

    [Fact]
    public void TextStoresStringIndexAlignmentAndFont()
    {
        var frame = new FrameBuilder()
            .AddText("a", 1, 2, Red)
            .AddText("b", 3, 4, Red, FontIndex.Heading, TextAlignment.End)
            .Build();

        Assert.Equal(["a", "b"], frame.Strings);
        var command = frame.Commands[1];
        Assert.Equal(DrawCommandType.Text, command.Type);
        Assert.Equal(1, command.Data);
        Assert.Equal((ushort)DrawCommandFlags.AlignEnd, command.Flags);
        Assert.Equal((int)FontIndex.Heading, frame.Styles[command.StyleIndex].FontIndex);
        Assert.Equal(Red.Rgba, frame.Styles[command.StyleIndex].FillColor);
    }

    [Fact]
    public void SpriteStoresIndexAndDestinationRectangle()
    {
        var frame = new FrameBuilder().AddSprite(17, 1, 2, 3, 4).Build();

        var command = Assert.Single(frame.Commands);
        Assert.Equal(DrawCommandType.Image, command.Type);
        Assert.Equal(17, command.Data);
        Assert.Equal(4, command.GeometryLength);
        Assert.Equal([1f, 2, 3, 4], frame.Geometry);
    }

    [Fact]
    public void SpriteRejectsBadArguments()
    {
        var builder = new FrameBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddSprite(-1, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddSprite(0, 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddSprite(0, float.NaN, 0, 1, 1));
    }

    [Fact]
    public void FailedAddLeavesFrameUntouched()
    {
        var builder = new FrameBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddSprite(0, float.NaN, 0, 1, 1));

        Assert.Empty(builder.Build().Commands);
    }
}
