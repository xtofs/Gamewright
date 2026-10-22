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
        canvas.DrawRectangle(10, 20, 30, 40, Red);

        var frame = canvas.Build();
        var command = Assert.Single(frame.Commands);
        Assert.Equal(DrawCommandType.Path, command.Type);
        Assert.Equal((ushort)DrawCommandFlags.Closed, command.Flags);
        Assert.Equal([10f, 20, 40, 20, 40, 60, 10, 60], frame.Geometry);
        Assert.Equal(PaintMode.Fill, frame.Paints[command.StyleIndex].Mode);
        Assert.Equal(Red.Rgba, frame.Paints[command.StyleIndex].FillRgba);
    }

    [Fact]
    public void RectangleOverloadDrawsTheSameAsTheFloatOverload()
    {
        var withRect = new Canvas(100, 100);
        withRect.DrawRectangle(new Rect(10, 20, 30, 40), Red);
        var withFloats = new Canvas(100, 100);
        withFloats.DrawRectangle(10, 20, 30, 40, Red);

        var (a, b) = (withRect.Build(), withFloats.Build());
        Assert.Equal(b.Geometry, a.Geometry);
        Assert.Equal(b.PathData, a.PathData);
        Assert.Equal(b.Paints, a.Paints);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Canvas(100, 100).DrawRectangle(new Rect(0, 0, 0, 5), Red));
    }

    [Fact]
    public void StrokeRectangleRecordsAStrokedPath()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawRectangle(0, 0, 10, 10, Outline);

        var frame = canvas.Build();
        var command = Assert.Single(frame.Commands);
        Assert.Equal((ushort)DrawCommandFlags.Closed, command.Flags);
        Assert.Equal(PaintMode.Stroke, frame.Paints[command.StyleIndex].Mode);
        Assert.Equal(2f, frame.Paints[command.StyleIndex].StrokeWidth);
    }

    [Fact]
    public void CircleIsTwoHalfArcsThroughTheLeftAndRightPoints()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawCircle(50, 40, 10, Red);

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
        canvas.DrawPath(path => path.MoveTo(0, 0).LineTo(5, 5).LineTo(0, 5).Close(), Red);
        canvas.DrawPath("M0 0 L5 5", Outline);

        Assert.Equal(2, canvas.Build().Commands.Length);
    }

    [Fact]
    public void TextAndSpritesAreRecordedInDrawOrder()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawRectangle(0, 0, 10, 10, Red);
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
    public void CenteredTextIsDrawnAtTheRectangleCenterWithCenterAlignment()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawTextCentered("A", new Rect(10, 20, 30, 40), Red, FontIndex.Heading);

        var frame = canvas.Build();
        var command = Assert.Single(frame.Commands);
        Assert.Equal(DrawCommandType.Text, command.Type);
        Assert.Equal([25f, 40], frame.Geometry);
        Assert.Equal((ushort)DrawCommandFlags.AlignCenter, command.Flags);
        Assert.Equal((int)FontIndex.Heading, frame.TextStyles[command.StyleIndex].Font);
        Assert.Equal(["A"], frame.Strings);
    }

    [Fact]
    public void CenteredTextRejectsEmptyRectangles()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawTextCentered("A", new Rect(0, 0, 0, 10), Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawTextCentered("A", new Rect(0, 0, 10, -1), Red));
    }

    [Fact]
    public void RejectsDegenerateShapes()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawRectangle(0, 0, 0, 10, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawRectangle(0, 0, 10, float.NaN, Outline));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawCircle(5, 5, 0, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawCircle(5, 5, -1, Outline));
    }

    [Fact]
    public void FillAndStrokeTogetherAreOneCommand()
    {
        var canvas = new Canvas(100, 100);
        canvas.DrawRectangle(0, 0, 10, 10, Paint.Fill(Red).WithStroke(new Color(0, 0, 255), 3));
        canvas.DrawCircle(5, 5, 4, Paint.Stroke(Red, 1).WithFill(new Color(0, 0, 255)));

        var frame = canvas.Build();
        Assert.Equal(2, frame.Commands.Length);
        Assert.Equal(
            [PaintMode.FillThenStroke, PaintMode.StrokeThenFill],
            frame.Commands.Select(c => frame.Paints[c.StyleIndex].Mode));
        Assert.Equal(8, frame.Commands[0].GeometryLength); // the rectangle's four points, once
    }

    [Fact]
    public void EmptyPaintCannotBeDrawn()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentException>(() => canvas.DrawRectangle(0, 0, 10, 10, default));
        Assert.Throws<ArgumentException>(() => canvas.DrawCircle(5, 5, 2, default));
        Assert.Throws<ArgumentException>(() => canvas.DrawPath("M0 0 L1 1", default));
    }

    [Fact]
    public void RejectsInvalidStrokeWidth()
    {
        var canvas = new Canvas(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawLine(0, 0, 1, 1, new StrokeStyle(Red, 0)));
    }
}
