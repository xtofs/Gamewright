using Gamewright;

namespace Gamewright.Tests;

public class FrameBuilderTests
{
    private static readonly Color Red = new(255, 0, 0);

    [Fact]
    public void PathNeedsFillOrStroke() =>
        Assert.Throws<ArgumentException>(() => new FrameBuilder().AddPath("M0 0 L1 1", default(Paint)));

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void StrokeWidthMustBePositiveAndFinite(float width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FrameBuilder().AddPath(path => path.MoveTo(0, 0).LineTo(1, 1), new StrokeStyle(Red, width)));

    [Fact]
    public void PathCommandPointsAtItsData()
    {
        var frame = new FrameBuilder()
            .AddPath(path => path.MoveTo(0, 0).LineTo(1, 1).LineTo(2, 0).Close(), Red)
            .AddPath("M5 5 L6 6", new StrokeStyle(Red, 2))
            .Build();

        var second = frame.Commands[1];
        Assert.Equal(DrawCommandType.Path, second.Type);
        Assert.Equal(6, second.GeometryOffset);
        Assert.Equal(4, second.GeometryLength);
        Assert.Equal(4, second.PathDataOffset);
        Assert.Equal(2, second.PathDataLength);
        Assert.Equal((ushort)DrawCommandFlags.None, second.Flags);
        Assert.Equal((ushort)DrawCommandFlags.Closed, frame.Commands[0].Flags);
        Assert.Equal(PaintMode.Stroke, frame.Paints[second.StyleIndex].Mode);
        Assert.Equal(2f, frame.Paints[second.StyleIndex].StrokeWidth);
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
        Assert.Equal((int)FontIndex.Heading, frame.TextStyles[command.StyleIndex].Font);
        Assert.Equal(Red.Rgba, frame.TextStyles[command.StyleIndex].ColorRgba);
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

    [Fact]
    public void IdenticalPaintsAreShared()
    {
        var frame = new FrameBuilder()
            .AddPath(path => path.MoveTo(0, 0).LineTo(1, 1), new StrokeStyle(Red, 2))
            .AddPath(path => path.MoveTo(2, 2).LineTo(3, 3), new StrokeStyle(Red, 2))
            .AddPath(path => path.MoveTo(4, 4).LineTo(5, 5), new StrokeStyle(Red, 3))
            .Build();

        Assert.Equal(2, frame.Paints.Length);
        Assert.Equal(frame.Commands[0].StyleIndex, frame.Commands[1].StyleIndex);
        Assert.NotEqual(frame.Commands[0].StyleIndex, frame.Commands[2].StyleIndex);
    }

    [Fact]
    public void IdenticalTextStylesAreSharedAndKeptApartFromPaints()
    {
        var frame = new FrameBuilder()
            .AddText("a", 0, 0, Red)
            .AddText("b", 0, 0, Red)
            .AddText("c", 0, 0, Red, FontIndex.Heading)
            .AddPath(path => path.MoveTo(0, 0).LineTo(1, 1), Red)
            .Build();

        Assert.Equal(2, frame.TextStyles.Length);
        Assert.Single(frame.Paints);
        Assert.Equal(frame.Commands[0].StyleIndex, frame.Commands[1].StyleIndex);
    }

    [Fact]
    public void BothPaintOrdersAreOneCommand()
    {
        var gold = new Color(215, 183, 126);
        var frame = new FrameBuilder()
            .AddPath("M0 0 L1 0 L1 1 Z", Paint.Fill(Red).WithStroke(gold, 4))
            .AddPath("M0 0 L1 0 L1 1 Z", Paint.Stroke(gold, 4).WithFill(Red))
            .Build();

        Assert.Equal(2, frame.Commands.Length);
        Assert.Equal(
            [PaintMode.FillThenStroke, PaintMode.StrokeThenFill],
            frame.Commands.Select(c => frame.Paints[c.StyleIndex].Mode));
    }

    [Fact]
    public void SpritesAddNoStyles()
    {
        var frame = new FrameBuilder().AddSprite(3, 0, 0, 10, 10).Build();

        Assert.Empty(frame.Paints);
        Assert.Empty(frame.TextStyles);
    }
}
