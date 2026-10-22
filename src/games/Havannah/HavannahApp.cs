namespace Havannah;

using Gamewright;
using Gamewright.Sprites;

/// <summary>
/// The Havannah board game as a <see cref="GameApp"/>: a hex board under a header with the status line
/// and a New game button. The board and header chrome are the static frame; pieces, win highlights and the
/// status text are the dynamic frame. See <see cref="HavannahGame"/> for the rules.
/// </summary>
public sealed class HavannahApp : GameApp
{
    private const int Width = 800;
    private const int HeaderHeight = 60;
    private const int BoardSide = 800;
    private const int Radius = 8;
    private const float BoardMargin = 15;
    private const float StatusX = 15;
    private const float StatusY = HeaderHeight / 2f;

    private static readonly Color BoardBackground = new(0x20, 0x20, 0x20);
    private static readonly Color CellOutline = new(0x87, 0xCE, 0xEB);
    private static readonly Color[] CellColors = OklabColorSpace.GrayTetrahedron(centerLightness: 0.39987f, s: 0.013f);
    private static readonly Color WinHighlight = new Color(0xFF, 0xFF, 0x00).WithAlpha(0.3f);
    private static readonly Color Ink = new(0xE6, 0xE6, 0xE6);
    private static readonly Color ButtonFill = new(0x3A, 0x3A, 0x3A);

    /// <summary>The "New game" button in the header.</summary>
    public static readonly Rect NewGameButton = new(Width - BoardMargin - 140, (HeaderHeight - 36) / 2f, 140, 36);

    private static readonly Rect BoardArea = new Rect(0, HeaderHeight, BoardSide, BoardSide).Deflate(BoardMargin);

    private readonly BoardGeometry geometry = new(BoardArea, Radius);

    private readonly SpriteAtlas<HavannahPiece> pieces = new(
        new SpriteSheet(new Image("pieces-spritesheet.png"), cellSize: 200),
        piece => piece switch
        {
            HavannahPiece.Red => (0, 0),
            HavannahPiece.Blue => (2, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, null)
        });

    private HavannahGame game = new(Radius);

    public HavannahApp() : base(Width, HeaderHeight + BoardSide, "Havannah")
    {
    }

    public HavannahGame Game => game;

    /// <summary>Where the hexes are, in frame coordinates.</summary>
    public BoardGeometry Geometry => geometry;

    /// <summary>The line shown in the header: whose turn it is, or who has just won and how.</summary>
    public string StatusText => game.Wins.Count > 0
        ? $"{game.Wins[0].Group} wins: {string.Join(", ", game.Wins.Select(win => win.Kind.ToString().ToLowerInvariant()))}"
        : $"{game.ActivePlayer} to play";

    public Invalidation NewGame()
    {
        game = new HavannahGame(Radius);
        return Invalidation.Dynamic;
    }

    protected override void DrawStatic(Canvas canvas)
    {
        canvas.DrawRectangle(new Rect(0, 0, FrameWidth, FrameHeight), BoardBackground);

        // One path per color class: the cells of a class never touch, so they can share a fill.
        for (var colorClass = 0; colorClass < CellColors.Length; colorClass++)
        {
            var cells = geometry.GridHexes.Where(hex => hex.ColorClass == colorClass).ToArray();
            canvas.DrawPath(path => AddHexagons(path, cells), CellColors[colorClass]);
        }

        // Strokes go after all fills so no fill covers half of a line. Shared edges are drawn once.
        canvas.DrawPath(path =>
        {
            foreach (var (a, b) in geometry.GridLines)
                path.MoveTo(a.X, a.Y).LineTo(b.X, b.Y);
        }, new StrokeStyle(CellOutline, 1.5f));

        canvas.DrawRectangle(NewGameButton, Paint.Fill(ButtonFill).WithStroke(CellOutline, 1));
        canvas.DrawTextCentered("New game", NewGameButton, Ink);
    }

    protected override void Draw(Canvas canvas)
    {
        canvas.DrawText(StatusText, StatusX, StatusY, Ink, FontIndex.Heading);

        foreach (var win in game.Wins)
        {
            var cells = win.Cells;
            canvas.DrawPath(path => AddHexagons(path, cells), WinHighlight);
        }

        var pieceSize = Size.Square(geometry.HexagonCircumRadius * 1.6f);
        foreach (var hex in geometry.GridHexes)
        {
            if (game[hex] is { } piece)
                canvas.DrawSprite(pieces[piece], Rect.FromCenter(geometry.HexCenter(hex), pieceSize));
        }
    }

    protected override Invalidation OnClick(float x, float y)
    {
        var point = new Point(x, y);
        if (NewGameButton.Contains(point))
            return NewGame();

        if (geometry.TryGetHex(point, out var hex) && game[hex] is null)
        {
            game.Play(hex);
            return Invalidation.Dynamic;
        }

        return Invalidation.None;
    }

    private void AddHexagons(PathBuilder path, IEnumerable<Hex> hexes)
    {
        foreach (var hex in hexes)
        {
            var corners = geometry.HexCorners(hex);
            path.MoveTo(corners[0].X, corners[0].Y);
            for (var i = 1; i < 6; i++)
                path.LineTo(corners[i].X, corners[i].Y);
            path.Close();
        }
    }
}
