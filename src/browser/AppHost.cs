using System.Runtime.InteropServices.JavaScript;
using Gamewright;

// Hosts the chess app in the page: sends its frames to the renderer and forwards input from
// main.ts. Only the frames named by an Invalidation are drawn and sent again.
partial class AppHost
{
    private static readonly ChessApp app = new();

    public static int FrameWidth => app.FrameWidth;

    public static int FrameHeight => app.FrameHeight;

    public static void Render(Invalidation invalidation)
    {
        if (invalidation.HasFlag(Invalidation.Static))
            Api.SendStatic(app.RenderStatic());
        if (invalidation.HasFlag(Invalidation.Dynamic))
            Api.SendDynamic(app.RenderDynamic());
    }

    /// <summary>Handles a click at a frame-space position.</summary>
    [JSExport]
    internal static void Click(double x, double y) => Render(app.Click((float)x, (float)y));

    [JSExport]
    internal static void NewGame() => Render(app.NewGame());
}
