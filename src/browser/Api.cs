using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

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

    [JSImport("dom.configureFrame", "main.js")]
    internal static partial void ConfigureFrame(int width, int height);

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
