using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

// The C# to JavaScript half of the boundary: calls into the renderer and the page in main.ts.
// The JavaScript to C# half (input) is the [JSExport] methods in AppHost.cs.
partial class AppHost
{
    // The byte spans are views into WebAssembly memory that are only valid during the call;
    // the renderer must copy what it keeps.
    [JSImport("dom.submitFrame", "main.js")]
    private static partial void SubmitFrame(
        bool isStatic,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> commands,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> pathData,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> geometry,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> paints,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> textStyles,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] strings);

    [JSImport("dom.configureFrame", "main.js")]
    private static partial void ConfigureFrame(int width, int height);

    [JSImport("dom.configureSprites", "main.js")]
    private static partial void ConfigureSprites(string imagePath, int columns, int cellSize);

    [JSImport("dom.reportError", "main.js")]
    private static partial void ReportError(string message);

    private static void SendFrame(bool isStatic, Frame frame) => SubmitFrame(
        isStatic,
        MemoryMarshal.AsBytes(frame.Commands.AsSpan()),
        frame.PathData,
        MemoryMarshal.AsBytes(frame.Geometry.AsSpan()),
        MemoryMarshal.AsBytes(frame.Paints.AsSpan()),
        MemoryMarshal.AsBytes(frame.TextStyles.AsSpan()),
        frame.Strings);
}
