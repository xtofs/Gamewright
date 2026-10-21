using Gamewright;

namespace Gamewright.Tests;

public class CanvasTests
{
    private static readonly Color Red = new(255, 0, 0);
    private static readonly StrokeStyle Outline = new(Red, 2);

    private static PathOpcode[] Ops(Frame frame) => [.. frame.PathData.Select(static b => (PathOpcode)b)];

    [Fact]
    public void ReportsItsSize()
    {
        var canvas = new Canvas(640, 620);

        Assert.Equal(640, canvas.Width);
        Assert.Equal(620, canvas.Height);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, -1)]
    public void SizeMustBePositive(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Canvas(width, height));

    [Fact]
    public void FillRectangleRecordsAFilledClosedPath()
    {
        var canvas = new Canvas(100, 100);
        canvas.FillRectangle(10, 20, 30, 40, Red);

        var frame = canvas.Build();
        var command = Assert.Single(frame.Commands);
        Assert.Equal(DrawCommandType.Path, command.Type);
        Assert.Equal((ushort)(DrawCommandFlags.Fill | DrawCommandFlags.Closed), command.Flags);
        Assert.Equal([10f, 20, 40, 20, 40, 60, 10, 60], frame.Geometry);
        Assert.Equal(Red.Rgba, frame.Styles[command.StyleIndex].FillColor);
    }

    [Fact]
    public void StrokeRectangleRecordsAStrokedPath()
    {
        var canvas = new Canvas(100, 100);
        canvas.StrokeRectangle(0, 0, 10, 10, Outline);

        var frame = canvas.Build();
        var command = Assert.Single(frame.Commands);
        Assert.Equal((ushort)(DrawCommandFlags.Stroke | DrawCommandFlags.Closed), command.Flags);
        Assert.Equal(2f, frame.Styles[command.StyleIndex].StrokeWidth);
    }

    [Fact]
    public void CircleIsTwoHalfArcsThroughTheLeftAndRightPoints()
    {
        var canvas = new Canvas(100, 100);
        canvas.FillCircle(50, 40, 10, Red);

        var frame = canvas.Build();
        Assert.Equal([PathOpcode.MoveTo, PathOpcode.ArcTo, PathOpcode.ArcTo, PathOpcode.ClosePath], Ops(frame));
        Assert.Equal(40f, frame.Geometry[0]);
        Assert.Equal(40f, frame.Geometry[1]);
        // First arc ends at the right-hand point, second returns to the left-hand one.
        Assert.Equal([60f, 40], frame.Geometry.Skip(2 + 5).Take(2).ToArray());
        Assert.Equal([40f, 40], frame.Geometry.Skip(2 + 7 + 5).Take(2).ToArray());
    }

    [Fact]
    public void LineIsAStrokedTwoPointPath()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawLine(1, 2, 3, 4, Outline);

        var frame = canvas.Build();
        Assert.Equal([PathOpcode.MoveTo, PathOpcode.LineTo], Ops(frame));
        Assert.Equal([1f, 2, 3, 4], frame.Geometry);
    }

    [Fact]
    public void PathsCanBeBuiltOrParsed()
    {
        var canvas = new Canvas(100, 100);
        canvas.FillPath(path => path.MoveTo(0, 0).LineTo(5, 5).LineTo(0, 5).Close(), Red);
        canvas.StrokePath("M0 0 L5 5", Outline);

        Assert.Equal(2, canvas.Build().Commands.Length);
    }

    [Fact]
    public void TextAndSpritesAreRecordedInDrawOrder()
    {
        var canvas = new Canvas(100, 100);
        canvas.FillRectangle(0, 0, 10, 10, Red);
        canvas.DrawSprite(14, 1, 2, 3, 4);
        canvas.DrawText("hi", 5, 6, Red, FontIndex.Heading, TextAlignment.Center);

        var frame = canvas.Build();
        Assert.Equal(
            [DrawCommandType.Path, DrawCommandType.Image, DrawCommandType.Text],
            frame.Commands.Select(static c => c.Type));
        Assert.Equal(14, frame.Commands[1].Data);
        Assert.Equal(["hi"], frame.Strings);
    }

    [Fact]
    public void RejectsDegenerateShapes()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.FillRectangle(0, 0, 0, 10, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.StrokeRectangle(0, 0, 10, float.NaN, Outline));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.FillCircle(5, 5, 0, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.StrokeCircle(5, 5, -1, Outline));
    }

    [Fact]
    public void RejectsInvalidStrokeWidth()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawLine(0, 0, 1, 1, new StrokeStyle(Red, 0)));
    }
}
