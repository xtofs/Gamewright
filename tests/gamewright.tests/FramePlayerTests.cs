using Gamewright;

namespace Gamewright.Tests;

public class FramePlayerTests
{
    private static readonly Color Red = new(255, 0, 0);
    private static readonly Color Blue = new(0, 0, 255);

    private static List<string> Draw(Action<Canvas> draw)
    {
        var canvas = new Canvas(100, 100);
        draw(canvas);
        return RecordingFrameTarget.Play(canvas.Build());
    }

    // A frame with one path command made by hand, to exercise encodings the builders never produce.
    private static Frame PathFrame(PathOpcode[] operations, float[] geometry, Paint? paint = null, DrawCommandFlags flags = DrawCommandFlags.None) =>
        new(
            [new DrawCommand(DrawCommandType.Path, 0, (ushort)flags, 0, geometry.Length, 0, operations.Length, 0)],
            [.. operations.Select(static o => (byte)o)],
            geometry,
            [paint ?? Paint.Fill(Red)],
            [],
            []);

    [Fact]
    public void FilledRectangleIsOneClosedSubpathThenAFill()
    {
        var calls = Draw(c => c.DrawRectangle(10, 20, 30, 40, Red));

        Assert.Equal(
            ["begin", "M 10 20", "L 40 20", "L 40 60", "L 10 60", "Z", "fill ff0000ff"],
            calls);
    }

    [Fact]
    public void PaintModesComeOutInPaintOrder()
    {
        string[] Paint(Paint paint) =>
            [.. Draw(c => c.DrawRectangle(0, 0, 1, 1, paint)).Where(static call => call.StartsWith("fill") || call.StartsWith("stroke"))];

        Assert.Equal(["fill ff0000ff"], Paint(Gamewright.Paint.Fill(Red)));
        Assert.Equal(["stroke ffff0000 3"], Paint(Gamewright.Paint.Stroke(Blue, 3)));
        Assert.Equal(["fill ff0000ff", "stroke ffff0000 3"], Paint(Gamewright.Paint.Fill(Red).WithStroke(Blue, 3)));
        Assert.Equal(["stroke ffff0000 3", "fill ff0000ff"], Paint(Gamewright.Paint.Stroke(Blue, 3).WithFill(Red)));
    }

    [Fact]
    public void CurvesPassThroughUnchanged()
    {
        var calls = Draw(c => c.DrawPath(
            path => path.MoveTo(0, 0).QuadTo(1, 2, 3, 4).CubicTo(5, 6, 7, 8, 9, 10), Red));

        Assert.Equal(["begin", "M 0 0", "Q 1 2 3 4", "C 5 6 7 8 9 10", "fill ff0000ff"], calls);
    }

    [Fact]
    public void ArcsKeepTheirFlagsAndRotation()
    {
        var calls = Draw(c => c.DrawPath(path => path.MoveTo(0, 0).ArcTo(5, 6, 30, true, false, 10, 0), Red));

        Assert.Equal("A 5 6 30 1 0 10 0", calls[2]);
    }

    [Fact]
    public void ArcToTheSamePointIsDropped()
    {
        var calls = Draw(c => c.DrawPath(path => path.MoveTo(1, 1).ArcTo(5, 5, 0, false, true, 1, 1).LineTo(2, 2), Red));

        Assert.Equal(["begin", "M 1 1", "L 2 2", "fill ff0000ff"], calls);
    }

    [Fact]
    public void ArcWithAZeroRadiusBecomesALine()
    {
        var calls = Draw(c => c.DrawPath(path => path.MoveTo(0, 0).ArcTo(0, 5, 0, false, true, 4, 4), Red));

        Assert.Equal(["begin", "M 0 0", "L 4 4", "fill ff0000ff"], calls);
    }

    [Fact]
    public void ArcRadiiAreMadePositive()
    {
        var calls = Draw(c => c.DrawPath(path => path.MoveTo(0, 0).ArcTo(-5, -6, 0, false, true, 4, 4), Red));

        Assert.Equal("A 5 6 0 0 1 4 4", calls[2]);
    }

    [Fact]
    public void ACircleIsTwoArcsAndAClose()
    {
        var calls = Draw(c => c.DrawCircle(50, 40, 10, Red));

        Assert.Equal(
            ["begin", "M 40 40", "A 10 10 0 0 1 60 40", "A 10 10 0 0 1 40 40", "Z", "fill ff0000ff"],
            calls);
    }

    [Fact]
    public void DrawingAfterAClosePathStartsANewSubpathAtTheOldStart()
    {
        var calls = Draw(c => c.DrawPath(path => path.MoveTo(1, 1).LineTo(2, 2).Close().LineTo(3, 3), Red));

        Assert.Equal(["begin", "M 1 1", "L 2 2", "Z", "M 1 1", "L 3 3", "fill ff0000ff"], calls);
    }

    [Fact]
    public void SeveralSubpathsKeepTheirOrder()
    {
        var calls = Draw(c => c.DrawPath(
            path => path.MoveTo(0, 0).LineTo(1, 1).Close().MoveTo(5, 5).LineTo(6, 6).Close(), Red));

        Assert.Equal(["begin", "M 0 0", "L 1 1", "Z", "M 5 5", "L 6 6", "Z", "fill ff0000ff"], calls);
    }

    [Fact]
    public void TheClosedFlagClosesAnOpenFinalSubpath()
    {
        var frame = PathFrame(
            [PathOpcode.MoveTo, PathOpcode.LineTo], [0, 0, 1, 1], flags: DrawCommandFlags.Closed);

        Assert.Equal(["begin", "M 0 0", "L 1 1", "Z", "fill ff0000ff"], RecordingFrameTarget.Play(frame));
    }

    [Fact]
    public void TheClosedFlagDoesNotCloseTwice()
    {
        var frame = PathFrame(
            [PathOpcode.MoveTo, PathOpcode.LineTo, PathOpcode.ClosePath], [0, 0, 1, 1], flags: DrawCommandFlags.Closed);

        Assert.Equal(["begin", "M 0 0", "L 1 1", "Z", "fill ff0000ff"], RecordingFrameTarget.Play(frame));
    }

    [Fact]
    public void TextCarriesItsStyleAlignmentAndPosition()
    {
        var calls = Draw(c =>
        {
            c.DrawText("hi", 5, 6, Red, FontIndex.Heading, TextAlignment.End);
            c.DrawTextCentered("mid", new Rect(0, 0, 10, 20), Blue);
        });

        Assert.Equal(
            ["text \"hi\" 5 6 ff0000ff font1 End", "text \"mid\" 5 10 ffff0000 font0 Center"],
            calls);
    }

    [Fact]
    public void SpritesPassTheirIndexAndRectangle()
    {
        var calls = Draw(c => c.DrawSprite(14, 1, 2, 3, 4));

        Assert.Equal(["sprite 14 1 2 3 4"], calls);
    }

    [Fact]
    public void CommandsPlayInDrawingOrder()
    {
        var calls = Draw(c =>
        {
            c.DrawRectangle(0, 0, 10, 10, Red);
            c.DrawSprite(1, 0, 0, 5, 5);
            c.DrawText("x", 1, 1, Red);
        });

        Assert.Equal(["begin", "sprite", "text"], calls.Where(static c => c is "begin" || c.StartsWith("sprite") || c.StartsWith("text"))
            .Select(static c => c.Split(' ')[0]));
    }

    [Fact]
    public void AnEmptyFrameDoesNothing() =>
        Assert.Empty(RecordingFrameTarget.Play(new Canvas(10, 10).Build()));

    [Fact]
    public void ANullFrameIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => FramePlayer.Play(null!, new RecordingFrameTarget()));

    public static TheoryData<string, Frame> MalformedFrames()
    {
        Paint[] paints = [Paint.Fill(Red)];
        DrawCommand Path(int pathOffset, int pathLength, int geometryOffset, int geometryLength, ushort style = 0) =>
            new(DrawCommandType.Path, style, 0, geometryOffset, geometryLength, pathOffset, pathLength, 0);

        return new TheoryData<string, Frame>
        {
            { "paint index", new Frame([Path(0, 1, 0, 2, style: 5)], [(byte)PathOpcode.MoveTo], [0, 0], paints, [], []) },
            { "empty paint", new Frame([Path(0, 1, 0, 2)], [(byte)PathOpcode.MoveTo], [0, 0], [default], [], []) },
            { "path range", new Frame([Path(0, 5, 0, 2)], [(byte)PathOpcode.MoveTo], [0, 0], paints, [], []) },
            { "geometry range", new Frame([Path(0, 1, 0, 9)], [(byte)PathOpcode.MoveTo], [0, 0], paints, [], []) },
            { "missing coordinates", new Frame([Path(0, 1, 0, 1)], [(byte)PathOpcode.MoveTo], [0], paints, [], []) },
            { "too much geometry", new Frame([Path(0, 1, 0, 4)], [(byte)PathOpcode.MoveTo], [0, 0, 1, 1], paints, [], []) },
            { "unknown opcode", new Frame([Path(0, 1, 0, 0)], [99], [], paints, [], []) },
            { "no move first", new Frame([Path(0, 1, 0, 2)], [(byte)PathOpcode.LineTo], [1, 1], paints, [], []) },
            { "command type", new Frame([new DrawCommand((DrawCommandType)9, 0, 0, 0, 0, 0, 0, 0)], [], [], paints, [], []) },
            { "text style", new Frame([new DrawCommand(DrawCommandType.Text, 3, 0, 0, 2, 0, 0, 0)], [], [0, 0], [], [new TextStyle(0, 0)], ["a"]) },
            { "text string", new Frame([new DrawCommand(DrawCommandType.Text, 0, 0, 0, 2, 0, 0, 4)], [], [0, 0], [], [new TextStyle(0, 0)], ["a"]) },
            { "text geometry", new Frame([new DrawCommand(DrawCommandType.Text, 0, 0, 0, 1, 0, 0, 0)], [], [0], [], [new TextStyle(0, 0)], ["a"]) },
            { "sprite geometry", new Frame([new DrawCommand(DrawCommandType.Image, 0, 0, 0, 3, 0, 0, 0)], [], [0, 0, 1], [], [], []) },
        };
    }

    [Theory]
    [MemberData(nameof(MalformedFrames))]
    public void MalformedFramesAreRejected(string name, Frame frame)
    {
        Assert.NotNull(name);
        Assert.Throws<InvalidDataException>(() => RecordingFrameTarget.Play(frame));
    }
}
