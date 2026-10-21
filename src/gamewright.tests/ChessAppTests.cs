using Gamewright;

namespace Gamewright.Tests;

public class ChessAppTests
{
    // Frame coordinates of a square's centre, from the app's layout (board at 112,104, 52px cells).
    private static (float X, float Y) Centre(string name) =>
        (112 + (name[0] - 'a') * 52 + 26, 104 + (8 - (name[1] - '0')) * 52 + 26);

    private static Invalidation Click(ChessApp app, string square)
    {
        var (x, y) = Centre(square);
        return app.Click(x, y);
    }

    private static int Count(Frame frame, DrawCommandType type) => frame.Commands.Count(c => c.Type == type);

    [Fact]
    public void DeclaresTheFrameSize()
    {
        var app = new ChessApp();

        Assert.Equal(640, app.FrameWidth);
        Assert.Equal(620, app.FrameHeight);
    }

    [Fact]
    public void StaticFrameHasTheBoardAndNoPieces()
    {
        var frame = new ChessApp().RenderStatic();

        Assert.Equal(0, Count(frame, DrawCommandType.Image));
        Assert.Equal(66, Count(frame, DrawCommandType.Path)); // border, 64 squares, outline
        Assert.Contains("GAMEWRIGHT  /  CHESS", frame.Strings);
    }

    [Fact]
    public void FileAndRankLabelsAreCentredInTheFrameMargin()
    {
        var frame = new ChessApp().RenderStatic();

        (float X, float Y) LabelPosition(string label)
        {
            var command = frame.Commands.Single(c => c.Type == DrawCommandType.Text && frame.Strings[c.Data] == label);
            return (frame.Geometry[command.GeometryOffset], frame.Geometry[command.GeometryOffset + 1]);
        }

        // Board at (112, 104), 52px cells, 34px margin: "a" is under the a-file, "8" beside the top rank.
        Assert.Equal((138f, 104f + 416 + 17), LabelPosition("a"));
        Assert.Equal((112f - 17, 104f + 26), LabelPosition("8"));
        Assert.Equal((112f + 7 * 52 + 26, 104f + 416 + 17), LabelPosition("h"));
        Assert.Equal((112f - 17, 104f + 7 * 52 + 26), LabelPosition("1"));
    }

    [Fact]
    public void DynamicFrameHasAllPiecesAndWhoseTurnItIs()
    {
        var frame = new ChessApp().RenderDynamic();

        Assert.Equal(32, Count(frame, DrawCommandType.Image));
        Assert.Contains("White to move", frame.Strings);
    }

    [Fact]
    public void SelectingAPieceInvalidatesOnlyTheDynamicFrame()
    {
        var app = new ChessApp();

        Assert.Equal(Invalidation.Dynamic, Click(app, "e2"));
        Assert.Equal(app.Board.Selected, new Square(4, 6));
        Assert.Equal(1, Count(app.RenderDynamic(), DrawCommandType.Path)); // the selection highlight
    }

    [Fact]
    public void MovingUpdatesTheFrameAndTheStatus()
    {
        var app = new ChessApp();
        Click(app, "e2");

        Assert.Equal(Invalidation.Dynamic, Click(app, "e4"));

        var frame = app.RenderDynamic();
        Assert.Contains("Black to move", frame.Strings);
        Assert.Contains("White Pawn: e2–e4", frame.Strings);
        Assert.Equal(32, Count(frame, DrawCommandType.Image));
    }

    [Fact]
    public void CapturesAreWrittenWithAnX()
    {
        var app = new ChessApp();
        Click(app, "d1");
        Click(app, "d7");

        var frame = app.RenderDynamic();
        Assert.Contains("White Queen: d1xd7", frame.Strings);
        Assert.Equal(31, Count(frame, DrawCommandType.Image));
    }

    [Theory]
    [InlineData(10f, 10f)]    // outside the board
    [InlineData(700f, 300f)]  // outside the frame
    [InlineData(-5f, -5f)]
    public void ClicksOffTheBoardChangeNothing(float x, float y) =>
        Assert.Equal(Invalidation.None, new ChessApp().Click(x, y));

    [Theory]
    [InlineData("e4")] // empty square
    [InlineData("e7")] // opponent's piece
    public void IgnoredClicksInvalidateNothing(string square) =>
        Assert.Equal(Invalidation.None, Click(new ChessApp(), square));

    [Fact]
    public void NewGameResetsTheBoard()
    {
        var app = new ChessApp();
        Click(app, "e2");
        Click(app, "e4");

        Assert.Equal(Invalidation.Dynamic, app.NewGame());

        Assert.Equal(PieceColor.White, app.Board.Turn);
        Assert.Null(app.Board.LastMove);
        Assert.Contains("White to move", app.RenderDynamic().Strings);
    }
}
