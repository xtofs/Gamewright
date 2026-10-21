using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "start")
            ChessSample.Start();

        try
        {
            Api.ConfigureSprites(SpriteSheet.Columns, SpriteSheet.CellSize);
            ChessSample.RenderStatic();
            while (true)
            {
                ChessSample.Render();
                await Task.Delay(100);
            }
        }
        catch (Exception exception)
        {
            Api.ReportError(exception.ToString());
            throw;
        }
    }
}

partial class Api
{
    // The byte spans are views into WebAssembly memory that are only valid during the call;
    // the renderer must copy what it keeps.
    [JSImport("dom.submitFrame", "main.js")]
    private static partial void SubmitFrame(
        bool isStatic,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> commands,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> pathData,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> geometry,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> styles,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] strings);

    [JSImport("dom.configureSprites", "main.js")]
    internal static partial void ConfigureSprites(int columns, int cellSize);

    [JSImport("dom.reportError", "main.js")]
    internal static partial void ReportError(string message);

    internal static void SendStatic(Frame frame) => Send(true, frame);

    internal static void SendDynamic(Frame frame) => Send(false, frame);

    private static void Send(bool isStatic, Frame frame) => SubmitFrame(
        isStatic,
        MemoryMarshal.AsBytes(frame.Commands.AsSpan()),
        frame.PathData,
        MemoryMarshal.AsBytes(frame.Geometry.AsSpan()),
        MemoryMarshal.AsBytes(frame.Styles.AsSpan()),
        frame.Strings);
}

partial class ChessSample
{
    private static readonly Stopwatch stopwatch = new();
    private const float BoardX = 112;
    private const float BoardY = 104;
    private const float CellSize = 52;
    private static readonly Color DarkSquare = new Color(91, 119, 92);
    private static readonly Color LightSquare = new Color(223, 214, 190);

    public static void Start() => stopwatch.Start();

    public static void RenderStatic()
    {
        var builder = new FrameBuilder();
        builder.AddPath(
            Rectangle(BoardX - 18, BoardY - 18, CellSize * 8 + 36, CellSize * 8 + 36),
            fillColor: new Color(93, 66, 45),
            strokeStyle: new StrokeStyle(new Color(215, 183, 126), 4));

        for (var rank = 0; rank < 8; rank++)
        {
            for (var file = 0; file < 8; file++)
            {
                var color = (rank + file) % 2 == 0 ? LightSquare : DarkSquare;
                builder.AddPath(
                    Rectangle(BoardX + file * CellSize, BoardY + rank * CellSize, CellSize, CellSize),
                    fillColor: color);
            }
        }

        builder.AddPath(
            Rectangle(BoardX, BoardY, CellSize * 8, CellSize * 8),
            strokeStyle: new StrokeStyle(new Color(38, 43, 39), 3));
        for (var index = 0; index < 8; index++)
        {
            var centerX = BoardX + index * CellSize + CellSize / 2;
            var centerY = BoardY + index * CellSize + CellSize / 2;
            builder.AddText(((char)('a' + index)).ToString(), centerX, BoardY + CellSize * 8 + 19,
                new Color(235, 220, 187), alignment: TextAlignment.Center);
            builder.AddText((8 - index).ToString(), BoardX - 20, centerY,
                new Color(235, 220, 187), alignment: TextAlignment.Center);
        }
        AddChessPieces(builder);
        builder.AddText("GAMEWRIGHT  /  CHESS", BoardX, 43, new Color(235, 220, 187), FontIndex.Heading);
        Api.SendStatic(builder.Build());
    }

    // Everything in the dynamic frame is a function of the whole elapsed second.
    private static int renderedSecond = -1;

    public static void Render()
    {
        var elapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds;
        if (elapsedSeconds == renderedSecond)
            return;
        renderedSecond = elapsedSeconds;

        var builder = new FrameBuilder();
        var selectedFile = elapsedSeconds / 2 % 8;
        var selectedColor = elapsedSeconds % 2 == 0
            ? new Color(231, 194, 77, 140)
            : new Color(231, 194, 77, 80);
        builder.AddPath(
            Rectangle(BoardX + selectedFile * CellSize, BoardY + 4 * CellSize, CellSize, CellSize),
            fillColor: selectedColor);

        builder.AddText($"Elapsed: {stopwatch.Elapsed:mm\\:ss}", BoardX + CellSize * 8, 43,
            new Color(235, 220, 187), FontIndex.Body, TextAlignment.End);
        Api.SendDynamic(builder.Build());
    }

    private const float PieceSize = 44;

    private static readonly PieceKind[] BackRank =
        [PieceKind.Rook, PieceKind.Knight, PieceKind.Bishop, PieceKind.Queen,
         PieceKind.King, PieceKind.Bishop, PieceKind.Knight, PieceKind.Rook];

    private static void AddChessPieces(FrameBuilder builder)
    {
        for (var file = 0; file < 8; file++)
        {
            AddPiece(builder, file, 0, PieceColor.Black, BackRank[file]);
            AddPiece(builder, file, 1, PieceColor.Black, PieceKind.Pawn);
            AddPiece(builder, file, 6, PieceColor.White, PieceKind.Pawn);
            AddPiece(builder, file, 7, PieceColor.White, BackRank[file]);
        }
    }

    private static void AddPiece(FrameBuilder builder, int file, int rank, PieceColor color, PieceKind kind)
    {
        var x = BoardX + file * CellSize + (CellSize - PieceSize) / 2;
        var y = BoardY + rank * CellSize + (CellSize - PieceSize) / 2;
        builder.AddSprite(ChessSprites.Index(color, kind), x, y, PieceSize, PieceSize);
    }

    private static Action<PathBuilder> Rectangle(float x, float y, float width, float height) =>
        path => path.MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height)
            .LineTo(x, y + height).Close();

    [JSExport]
    internal static bool Toggle()
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
            return false;
        }

        stopwatch.Start();
        return true;
    }

    [JSExport]
    internal static void Reset()
    {
        if (stopwatch.IsRunning)
            stopwatch.Restart();
        else
            stopwatch.Reset();

        renderedSecond = -1;
        Render();
    }

    [JSExport]
    internal static bool IsRunning() => stopwatch.IsRunning;
}
