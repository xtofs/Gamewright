using Gamewright;

namespace Gamewright.Browser;

// Hosts a GameApp in the page: sends its frames to the renderer and receives input from main.ts.
// Only the frames named by an Invalidation are drawn and sent again. The calls into JavaScript
// are in AppHost.Interop.cs.
public sealed partial class AppHost
{
    private readonly GameApp app;

    // Kept in a field so the delegate handed to JavaScript stays alive as long as the host.
    private readonly Action<double, double> clickHandler;

    public AppHost(GameApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        this.app = app;
        clickHandler = OnClick;
    }

    /// <summary>Configures the renderer for the app, starts listening for input and draws the first frames. Failures are shown on the page.</summary>
    public void Start()
    {
        try
        {
            ConfigureFrame(app.FrameWidth, app.FrameHeight);
            SetClickHandler(clickHandler);
            Render(Invalidation.All);
        }
        catch (Exception exception)
        {
            ReportError(exception.ToString());
            throw;
        }
    }

    /// <summary>Handles a click at a frame-space position.</summary>
    private void OnClick(double x, double y) => Render(app.Click((float)x, (float)y));

    private void Render(Invalidation invalidation)
    {
        if (invalidation.HasFlag(Invalidation.Static))
            SendFrame(true, app.RenderStatic());
        if (invalidation.HasFlag(Invalidation.Dynamic))
            SendFrame(false, app.RenderDynamic());
    }
}
