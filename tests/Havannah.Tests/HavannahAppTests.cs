using Gamewright;

namespace Havannah.Tests;

public class HavannahAppTests
{
    private static readonly Hex Center = (0, 0, 0);
    private static readonly Hex[] AroundCenter =
        [(1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1)];

    private static Invalidation ClickHex(HavannahApp app, Hex hex)
    {
        var center = app.Geometry.HexCenter(hex);
        return app.Click(center.X, center.Y);
    }

    private static Invalidation ClickPoint(HavannahApp app, Point point) => app.Click(point.X, point.Y);

    private static int Count(Frame frame, DrawCommandType type) => frame.Commands.Count(c => c.Type == type);

    [Fact]
    public void DeclaresItsFrameSize()
    {
        var app = new HavannahApp();

        Assert.Equal(800, app.FrameWidth);
        Assert.Equal(860, app.FrameHeight);
    }

    [Fact]
    public void StaticFrameHasTheBoardAndTheButton()
    {
        var frame = new HavannahApp().RenderStatic();

        // Background, four color classes of cells, the grid lines and the button.
        Assert.Equal(7, Count(frame, DrawCommandType.Path));
        Assert.Equal(0, Count(frame, DrawCommandType.Image));
        Assert.Equal(["New game"], frame.Strings);
    }

    [Fact]
    public void EveryCellIsFilledOnceAcrossTheColorClassPaths()
    {
        var frame = new HavannahApp().RenderStatic();

        // Each hexagon is MoveTo + 5 LineTo + Close: six corners of two floats each.
        var cellPaths = frame.Commands.Where(c => c.Type == DrawCommandType.Path)
            .Where(c => frame.Paints[c.StyleIndex].Mode == PaintMode.Fill)
            .Skip(1).Take(4).ToArray();

        Assert.Equal(169 * 12, cellPaths.Sum(c => c.GeometryLength));
    }

    [Fact]
    public void AnEmptyBoardShowsOnlyTheStatus()
    {
        var frame = new HavannahApp().RenderDynamic();

        Assert.Equal(["Red to play"], frame.Strings);
        Assert.Equal(0, Count(frame, DrawCommandType.Image));
        Assert.Equal(0, Count(frame, DrawCommandType.Path));
    }

    [Fact]
    public void ClickingAnEmptyCellPlaysItWithTheActivePlayersPiece()
    {
        var app = new HavannahApp();

        Assert.Equal(Invalidation.Dynamic, ClickHex(app, Center));

        Assert.Equal(HavannahPiece.Red, app.Game[Center]);
        var frame = app.RenderDynamic();
        Assert.Equal(["Blue to play"], frame.Strings);
        var image = Assert.Single(frame.Commands, c => c.Type == DrawCommandType.Image);
        Assert.Equal(["pieces-spritesheet.png"], frame.Images);
        // Red's piece is the cell at column 0, row 0 of the 200px grid.
        Assert.Equal([0f, 0, 200, 200], frame.Geometry.Skip(image.GeometryOffset).Take(4));
    }

    [Fact]
    public void BluePiecesUseTheirOwnCell()
    {
        var app = new HavannahApp();
        ClickHex(app, Center);
        ClickHex(app, AroundCenter[0]);

        var frame = app.RenderDynamic();

        var sources = frame.Commands.Where(c => c.Type == DrawCommandType.Image)
            .Select(c => frame.Geometry.Skip(c.GeometryOffset).Take(4).ToArray())
            .ToArray();
        Assert.Equal(2, sources.Length);
        Assert.Contains(new float[] { 0, 0, 200, 200 }, sources);
        Assert.Contains(new float[] { 400, 0, 200, 200 }, sources);
    }

    [Fact]
    public void PiecesAreDrawnCenteredOnTheirCellAndSmallerThanIt()
    {
        var app = new HavannahApp();
        ClickHex(app, AroundCenter[2]);

        var frame = app.RenderDynamic();

        var image = Assert.Single(frame.Commands, c => c.Type == DrawCommandType.Image);
        var g = frame.Geometry.Skip(image.GeometryOffset + 4).Take(4).ToArray();
        var center = app.Geometry.HexCenter(AroundCenter[2]);
        Assert.Equal(center.X, g[0] + g[2] / 2, 2);
        Assert.Equal(center.Y, g[1] + g[3] / 2, 2);
        Assert.Equal(app.Geometry.HexagonCircumRadius * 1.6f, g[2], 2);
    }

    [Fact]
    public void ClickingAnOccupiedCellDoesNothing()
    {
        var app = new HavannahApp();
        ClickHex(app, Center);

        Assert.Equal(Invalidation.None, ClickHex(app, Center));
        Assert.Equal(HavannahPiece.Blue, app.Game.ActivePlayer);
    }

    [Theory]
    [InlineData(5f, 5f)]       // header corner
    [InlineData(400f, 30f)]    // header, away from the button
    [InlineData(5f, 850f)]     // below the board, outside every hex
    [InlineData(-10f, 400f)]   // outside the frame
    [InlineData(900f, 900f)]
    public void ClicksOutsideTheBoardAndButtonDoNothing(float x, float y) =>
        Assert.Equal(Invalidation.None, new HavannahApp().Click(x, y));

    [Fact]
    public void PlayersAlternateAcrossClicks()
    {
        var app = new HavannahApp();

        ClickHex(app, AroundCenter[0]);
        ClickHex(app, AroundCenter[1]);
        ClickHex(app, AroundCenter[2]);

        Assert.Equal(
            [HavannahPiece.Red, HavannahPiece.Blue, HavannahPiece.Red],
            AroundCenter.Take(3).Select(h => app.Game[h]!.Value));
    }

    [Fact]
    public void EveryCellCanBeClickedAtItsCenter()
    {
        var app = new HavannahApp();

        foreach (var hex in app.Geometry.GridHexes)
            Assert.Equal(Invalidation.Dynamic, ClickHex(app, hex));

        Assert.All(app.Geometry.GridHexes, hex => Assert.NotNull(app.Game[hex]));
        Assert.Equal(169, Count(app.RenderDynamic(), DrawCommandType.Image));
    }

    [Fact]
    public void TheNewGameButtonResetsTheBoard()
    {
        var app = new HavannahApp();
        ClickHex(app, Center);
        ClickHex(app, AroundCenter[0]);

        var button = HavannahApp.NewGameButton.Center;

        Assert.Equal(Invalidation.Dynamic, ClickPoint(app, button));
        Assert.Null(app.Game[Center]);
        Assert.Equal(HavannahPiece.Red, app.Game.ActivePlayer);
        Assert.Equal(["Red to play"], app.RenderDynamic().Strings);
    }

    [Fact]
    public void TheButtonIsInsideTheHeaderAndTheFrame()
    {
        var button = HavannahApp.NewGameButton;

        Assert.True(button.X > 0 && button.Right < 800);
        Assert.True(button.Y > 0 && button.Bottom < 60);
    }

    [Fact]
    public void AWinShowsItsKindAndHighlightsItsCells()
    {
        var app = new HavannahApp();
        Hex[] blue = [(7, -7, 0), (-7, 7, 0), (0, 7, -7), (0, -7, 7), (7, 0, -7)];
        for (var i = 0; i < 5; i++)
        {
            ClickHex(app, AroundCenter[i]);
            ClickHex(app, blue[i]);
        }
        Assert.Equal("Red to play", app.StatusText);

        ClickHex(app, AroundCenter[5]);

        Assert.Equal("Red wins: ring", app.StatusText);
        var frame = app.RenderDynamic();
        Assert.Contains("Red wins: ring", frame.Strings);
        var highlight = Assert.Single(frame.Commands, c => c.Type == DrawCommandType.Path);
        Assert.Equal(6 * 12, highlight.GeometryLength); // the ring's six hexagons
        Assert.True(frame.Paints[highlight.StyleIndex].FillRgba >> 24 < 255, "the highlight is translucent");
        Assert.Equal(11, Count(frame, DrawCommandType.Image));
    }

    [Fact]
    public void TheHighlightGoesAwayWithTheNextMove()
    {
        var app = new HavannahApp();
        Hex[] blue = [(7, -7, 0), (-7, 7, 0), (0, 7, -7), (0, -7, 7), (7, 0, -7)];
        for (var i = 0; i < 6; i++)
        {
            ClickHex(app, AroundCenter[i]);
            if (i < 5) ClickHex(app, blue[i]);
        }

        ClickHex(app, (3, -3, 0));

        Assert.Equal(0, Count(app.RenderDynamic(), DrawCommandType.Path));
        Assert.EndsWith("to play", app.StatusText);
    }
}
