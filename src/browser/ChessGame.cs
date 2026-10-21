using System.Runtime.InteropServices.JavaScript;
using Gamewright;

// The browser-facing game: draws a ChessBoard into frames and turns clicks into board moves.
// Coordinates are in the 640x620 logical space that main.ts scales to the canvas.
partial class ChessGame
{
    private const float BoardX = 112;
    private const float BoardY = 104;
    private const float CellSize = 52;
    private const float BoardSize = CellSize * ChessBoard.Size;
    private const float FrameMargin = 34;
    private const float PieceSize = 44;
    private const float CenterX = 320;

    private static readonly Color DarkSquare = new(91, 119, 92);
    private static readonly Color LightSquare = new(223, 214, 190);
    private static readonly Color Selection = new(231, 194, 77, 150);
    private static readonly Color LastMoveTint = new(231, 194, 77, 70);
    private static readonly Color Ink = new(235, 220, 187);

    private static ChessBoard board = ChessBoard.NewGame();

    public static void RenderStatic()
    {
        var builder = new FrameBuilder();
        builder.AddPath(
            Rectangle(BoardX - FrameMargin, BoardY - FrameMargin, BoardSize + 2 * FrameMargin, BoardSize + 2 * FrameMargin),
            fillColor: new Color(93, 66, 45),
            strokeStyle: new StrokeStyle(new Color(215, 183, 126), 4));

        for (var rank = 0; rank < ChessBoard.Size; rank++)
        {
            for (var file = 0; file < ChessBoard.Size; file++)
            {
                var (x, y) = SquareOrigin(new Square(file, rank));
                builder.AddPath(
                    Rectangle(x, y, CellSize, CellSize),
                    fillColor: (rank + file) % 2 == 0 ? LightSquare : DarkSquare);
            }
        }

        builder.AddPath(
            Rectangle(BoardX, BoardY, BoardSize, BoardSize),
            strokeStyle: new StrokeStyle(new Color(38, 43, 39), 3));
        for (var index = 0; index < ChessBoard.Size; index++)
        {
            var center = index * CellSize + CellSize / 2;
            builder.AddText(((char)('a' + index)).ToString(), BoardX + center, BoardY + BoardSize + FrameMargin / 2,
                Ink, alignment: TextAlignment.Center);
            builder.AddText((ChessBoard.Size - index).ToString(), BoardX - FrameMargin / 2, BoardY + center,
                Ink, alignment: TextAlignment.Center);
        }
        builder.AddText("GAMEWRIGHT  /  CHESS", BoardX, 43, Ink, FontIndex.Heading);
        Api.SendStatic(builder.Build());
    }

    public static void Render()
    {
        var builder = new FrameBuilder();

        if (board.LastMove is { } last)
        {
            AddHighlight(builder, last.From, LastMoveTint);
            AddHighlight(builder, last.To, LastMoveTint);
        }
        if (board.Selected is { } selected)
            AddHighlight(builder, selected, Selection);

        foreach (var (square, piece) in board.Pieces())
        {
            var (x, y) = SquareOrigin(square);
            var inset = (CellSize - PieceSize) / 2;
            builder.AddSprite(ChessSprites.Index(piece.Color, piece.Kind), x + inset, y + inset, PieceSize, PieceSize);
        }

        builder.AddText($"{board.Turn} to move", BoardX + BoardSize, 43, Ink, FontIndex.Body, TextAlignment.End);
        if (board.LastMove is { } move)
        {
            var capture = move.Captured is null ? "\u2013" : "x";
            builder.AddText($"{move.Piece.Color} {move.Piece.Kind}: {move.From}{capture}{move.To}",
                CenterX, BoardY + BoardSize + FrameMargin + 22, Ink, FontIndex.Body, TextAlignment.Center);
        }
        Api.SendDynamic(builder.Build());
    }

    private static void AddHighlight(FrameBuilder builder, Square square, Color color)
    {
        var (x, y) = SquareOrigin(square);
        builder.AddPath(Rectangle(x, y, CellSize, CellSize), fillColor: color);
    }

    private static (float X, float Y) SquareOrigin(Square square) =>
        (BoardX + square.File * CellSize, BoardY + square.Rank * CellSize);

    private static Action<PathBuilder> Rectangle(float x, float y, float width, float height) =>
        path => path.MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height)
            .LineTo(x, y + height).Close();

    /// <summary>Handles a click at a logical-space position; clicks outside the board are ignored.</summary>
    [JSExport]
    internal static void Click(double x, double y)
    {
        var square = new Square(
            (int)Math.Floor((x - BoardX) / CellSize),
            (int)Math.Floor((y - BoardY) / CellSize));
        if (!square.IsOnBoard)
            return;

        if (board.Click(square) != ClickResult.Ignored)
            Render();
    }

    [JSExport]
    internal static void NewGame()
    {
        board = ChessBoard.NewGame();
        Render();
    }
}
