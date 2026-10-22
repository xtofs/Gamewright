namespace Chess;

using Gamewright;

/// <summary>A click-to-move chess board drawn with a <see cref="Canvas"/>; see <see cref="ChessBoard"/> for the rules (there are none).</summary>
public sealed class ChessApp : GameApp
{
    private const int Width = 640;
    private const int Height = 620;

    private const float BoardX = 112;
    private const float BoardY = 104;
    private const float CellSize = 52;
    private const float BoardSize = CellSize * ChessBoard.Size;
    private const float FrameMargin = 24;
    private const float PieceSize = 44;

    private static readonly Color DarkSquare = new(91, 119, 92);
    private static readonly Color LightSquare = new(223, 214, 190);
    private static readonly Color Selection = new(231, 194, 77, 150);
    private static readonly Color LastMoveTint = new(231, 194, 77, 70);
    private static readonly Color Ink = new(235, 220, 187);

    private ChessBoard board = ChessBoard.NewGame();

    public ChessApp() : base(Width, Height, ChessSprites.Sheet)
    {
    }

    public ChessBoard Board => board;

    public Invalidation NewGame()
    {
        board = ChessBoard.NewGame();
        return Invalidation.Dynamic;
    }

    private static readonly Paint BoardBackground = Paint.Fill(new Color(93, 66, 45));
    private static readonly StrokeStyle CheckerboardFrame = new StrokeStyle(new Color(38, 43, 39), 3);

    protected override void DrawStatic(Canvas canvas)
    {
        var rect = new Rect(BoardX - FrameMargin, BoardY - FrameMargin, BoardSize + 2 * FrameMargin, BoardSize + 2 * FrameMargin);
        canvas.DrawRectangle(rect, BoardBackground);

        for (var rank = 0; rank < ChessBoard.Size; rank++)
        {
            for (var file = 0; file < ChessBoard.Size; file++)
            {
                var (x, y) = SquareOrigin(new Square(file, rank));
                canvas.DrawRectangle(x, y, CellSize, CellSize, (rank + file) % 2 == 0 ? LightSquare : DarkSquare);
            }
        }

        canvas.DrawRectangle(BoardX, BoardY, BoardSize, BoardSize, CheckerboardFrame);
        for (var index = 0; index < ChessBoard.Size; index++)
        {
            // File letters sit in the frame margin under each column, rank numbers in the margin beside each row.
            canvas.DrawTextCentered(
                $"{(char)('a' + index)}",
                new Rect(BoardX + index * CellSize, BoardY + BoardSize, CellSize, FrameMargin),
                Ink);
            canvas.DrawTextCentered(
                $"{ChessBoard.Size - index}",
                new Rect(BoardX - FrameMargin, BoardY + index * CellSize, FrameMargin, CellSize),
                Ink);
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
        canvas.DrawRectangle(x, y, CellSize, CellSize, color);
    }

    private static (float X, float Y) SquareOrigin(Square square) =>
        (BoardX + square.File * CellSize, BoardY + square.Rank * CellSize);
}
