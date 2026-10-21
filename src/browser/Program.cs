using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "start")
            StopwatchSample.Start();

        StopwatchSample.RenderStatic();
        while (true)
        {
            StopwatchSample.Render();
            await Task.Delay(1000 / 30);
        }
    }
}

partial class Api
{
    [JSImport("dom.submitStatic", "main.js")]
    private static partial void SubmitStatic(
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] commands,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] pathData,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[] geometry,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] strings,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] styles);

    [JSImport("dom.submitDynamic", "main.js")]
    private static partial void SubmitDynamic(
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] commands,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] pathData,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[] geometry,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] strings,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] styles);

    internal static void SendStatic(Frame frame) => SubmitStatic(
        EncodeCommands(frame.Commands),
        Array.ConvertAll(frame.PathData, static value => (int)value),
        Array.ConvertAll(frame.Geometry, static value => (double)value),
        frame.Strings,
        EncodeStyles(frame.Styles));

    internal static void SendDynamic(Frame frame) => SubmitDynamic(
        EncodeCommands(frame.Commands),
        Array.ConvertAll(frame.PathData, static value => (int)value),
        Array.ConvertAll(frame.Geometry, static value => (double)value),
        frame.Strings,
        EncodeStyles(frame.Styles));

    private static int[] EncodeCommands(DrawCommand[] commands) => commands
        .SelectMany(static command => new[]
        {
            (int)command.Type,
            command.StyleIndex,
            command.Flags,
            command.GeometryOffset,
            command.GeometryLength,
            command.PathDataOffset,
            command.PathDataLength,
            command.Data
        })
        .ToArray();

    private static int[] EncodeStyles(Style[] styles) =>
        MemoryMarshal.AsBytes(styles.AsSpan()).ToArray().Select(static value => (int)value).ToArray();
}

partial class StopwatchSample
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

    public static void Render()
    {
        var builder = new FrameBuilder();
        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var selectedFile = (int)(elapsedSeconds / 2) % 8;
        var selectedColor = (int)elapsedSeconds % 2 == 0
            ? new Color(231, 194, 77, 140)
            : new Color(231, 194, 77, 80);
        builder.AddPath(
            Rectangle(BoardX + selectedFile * CellSize, BoardY + 4 * CellSize, CellSize, CellSize),
            fillColor: selectedColor);

        builder.AddText($"Elapsed: {stopwatch.Elapsed:mm\\:ss}", BoardX + CellSize * 8, 43,
            new Color(235, 220, 187), FontIndex.Body, TextAlignment.End);
        Api.SendDynamic(builder.Build());
    }

    // Sprite indices on sprites.png: 7 columns, rows of K, Q, R, B, N, P; white row 2, black row 3.
    private const int WhiteKingSprite = 14;
    private const int BlackKingSprite = 21;
    private const float PieceSize = 44;

    private static void AddChessPieces(FrameBuilder builder)
    {
        // Offsets within a sprite row: King 0, Queen 1, Rook 2, Bishop 3, Knight 4, Pawn 5.
        int[] backRank = [2, 4, 3, 1, 0, 3, 4, 2];
        for (var file = 0; file < 8; file++)
        {
            AddPiece(builder, file, 0, BlackKingSprite + backRank[file]);
            AddPiece(builder, file, 1, BlackKingSprite + 5);
            AddPiece(builder, file, 6, WhiteKingSprite + 5);
            AddPiece(builder, file, 7, WhiteKingSprite + backRank[file]);
        }
    }

    private static void AddPiece(FrameBuilder builder, int file, int rank, int sprite)
    {
        var x = BoardX + file * CellSize + (CellSize - PieceSize) / 2;
        var y = BoardY + rank * CellSize + (CellSize - PieceSize) / 2;
        builder.AddSprite(sprite, x, y, PieceSize, PieceSize);
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

        Render();
    }

    [JSExport]
    internal static bool IsRunning() => stopwatch.IsRunning;
}
