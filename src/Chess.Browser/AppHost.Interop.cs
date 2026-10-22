using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

// The calls into JavaScript: the renderer and the page in main.ts. Input comes the other way
// through the callback registered with SetClickHandler. The imports are static because the
// interop generator requires it, but they hold no state.
sealed partial class AppHost
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

    // JavaScript keeps the callback and calls it with the click position in frame coordinates.
    [JSImport("dom.setClickHandler", "main.js")]
    private static partial void SetClickHandler(
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> handler);

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
