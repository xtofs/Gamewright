using System.Runtime.InteropServices.JavaScript;
using Gamewright;

// Hosts a GameApp in the page: sends its frames to the renderer and forwards input from main.ts.
// Only the frames named by an Invalidation are drawn and sent again. The imports from JavaScript
// are in AppHost.Interop.cs.
partial class AppHost
{
    private static GameApp? app;

    /// <summary>Configures the renderer for the app and draws its first frames. Failures are shown on the page.</summary>
    public static void Start(GameApp gameApp)
    {
        ArgumentNullException.ThrowIfNull(gameApp);

        try
        {
            app = gameApp;
            ConfigureFrame(gameApp.FrameWidth, gameApp.FrameHeight);
            if (gameApp.SpriteSheet is { } sheet)
                ConfigureSprites(sheet.ImagePath, sheet.Columns, sheet.CellSize);
            Render(Invalidation.All);
        }
        catch (Exception exception)
        {
            ReportError(exception.ToString());
            throw;
        }
    }

    /// <summary>Handles a click at a frame-space position.</summary>
    [JSExport]
    internal static void Click(double x, double y) => Render(Running.Click((float)x, (float)y));

    private static GameApp Running =>
        app ?? throw new InvalidOperationException("The app has not been started.");

    private static void Render(Invalidation invalidation)
    {
        if (invalidation.HasFlag(Invalidation.Static))
            SendFrame(true, Running.RenderStatic());
        if (invalidation.HasFlag(Invalidation.Dynamic))
            SendFrame(false, Running.RenderDynamic());
    }
}
