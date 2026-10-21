namespace Gamewright;

/// <summary>A click-to-move chess board drawn with a <see cref="Canvas"/>; see <see cref="ChessBoard"/> for the rules (there are none).</summary>
public sealed class ChessApp : GameApp
{
    private const int Width = 640;
    private const int Height = 620;

    private const float BoardX = 112;
    private const float BoardY = 104;
    private const float CellSize = 52;
    private const float BoardSize = CellSize * ChessBoard.Size;
    private const float FrameMargin = 34;
    private const float PieceSize = 44;

    private static readonly Color DarkSquare = new(91, 119, 92);
    private static readonly Color LightSquare = new(223, 214, 190);
    private static readonly Color Selection = new(231, 194, 77, 150);
    private static readonly Color LastMoveTint = new(231, 194, 77, 70);
    private static readonly Color Ink = new(235, 220, 187);

    private ChessBoard board = ChessBoard.NewGame();

    public ChessApp() : base(Width, Height)
    {
    }

    public ChessBoard Board => board;

    public Invalidation NewGame()
    {
        board = ChessBoard.NewGame();
        return Invalidation.Dynamic;
    }

    protected override void DrawStatic(Canvas canvas)
    {
        canvas.FillRectangle(
            BoardX - FrameMargin, BoardY - FrameMargin, BoardSize + 2 * FrameMargin, BoardSize + 2 * FrameMargin,
            new Color(93, 66, 45));
        canvas.StrokeRectangle(
            BoardX - FrameMargin, BoardY - FrameMargin, BoardSize + 2 * FrameMargin, BoardSize + 2 * FrameMargin,
            new StrokeStyle(new Color(215, 183, 126), 4));

        for (var rank = 0; rank < ChessBoard.Size; rank++)
        {
            for (var file = 0; file < ChessBoard.Size; file++)
            {
                var (x, y) = SquareOrigin(new Square(file, rank));
                canvas.FillRectangle(x, y, CellSize, CellSize, (rank + file) % 2 == 0 ? LightSquare : DarkSquare);
            }
        }

        canvas.StrokeRectangle(BoardX, BoardY, BoardSize, BoardSize, new StrokeStyle(new Color(38, 43, 39), 3));
        for (var index = 0; index < ChessBoard.Size; index++)
        {
            var center = index * CellSize + CellSize / 2;
            canvas.DrawText(((char)('a' + index)).ToString(), BoardX + center, BoardY + BoardSize + FrameMargin / 2,
                Ink, alignment: TextAlignment.Center);
            canvas.DrawText((ChessBoard.Size - index).ToString(), BoardX - FrameMargin / 2, BoardY + center,
                Ink, alignment: TextAlignment.Center);
        }
        canvas.DrawText("GAMEWRIGHT  /  CHESS", BoardX, 43, Ink, FontIndex.Heading);
    }

    protected override void Draw(Canvas canvas)
    {
        if (board.LastMove is { } last)
        {
            Highlight(canvas, last.From, LastMoveTint);
            Highlight(canvas, last.To, LastMoveTint);
        }
        if (board.Selected is { } selected)
            Highlight(canvas, selected, Selection);

        var inset = (CellSize - PieceSize) / 2;
        foreach (var (square, piece) in board.Pieces())
        {
            var (x, y) = SquareOrigin(square);
            canvas.DrawSprite(ChessSprites.Index(piece.Color, piece.Kind), x + inset, y + inset, PieceSize, PieceSize);
        }

        canvas.DrawText($"{board.Turn} to move", BoardX + BoardSize, 43, Ink, FontIndex.Body, TextAlignment.End);
        if (board.LastMove is { } move)
        {
            var separator = move.Captured is null ? "–" : "x";
            canvas.DrawText($"{move.Piece.Color} {move.Piece.Kind}: {move.From}{separator}{move.To}",
                canvas.Width / 2f, BoardY + BoardSize + FrameMargin + 22, Ink, FontIndex.Body, TextAlignment.Center);
        }
    }

    protected override Invalidation OnClick(float x, float y)
    {
        var square = new Square(
            (int)Math.Floor((x - BoardX) / CellSize),
            (int)Math.Floor((y - BoardY) / CellSize));
        if (!square.IsOnBoard)
            return Invalidation.None;

        return board.Click(square) == ClickResult.Ignored ? Invalidation.None : Invalidation.Dynamic;
    }

    private static void Highlight(Canvas canvas, Square square, Color color)
    {
        var (x, y) = SquareOrigin(square);
        canvas.FillRectangle(x, y, CellSize, CellSize, color);
    }

    private static (float X, float Y) SquareOrigin(Square square) =>
        (BoardX + square.File * CellSize, BoardY + square.Rank * CellSize);
}
