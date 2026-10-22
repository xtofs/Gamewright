namespace Gamewright;

/// <summary>
/// Base class for an app that draws itself with two frames: a static one that changes rarely and
/// is cached by the renderer, and a dynamic one drawn over it. The host calls
/// <see cref="RenderStatic"/> and <see cref="RenderDynamic"/> at start-up and again whenever an
/// <see cref="Invalidation"/> says the matching frame is out of date, which input handlers return.
/// </summary>
public abstract class GameApp
{
    protected GameApp(int frameWidth, int frameHeight, SpriteSheetLayout? spriteSheet = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameHeight);
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        SpriteSheet = spriteSheet;
    }

    /// <summary>Width of the coordinate space the app draws in; the renderer scales it to the screen.</summary>
    public int FrameWidth { get; }

    public int FrameHeight { get; }

    /// <summary>Layout of the sprite sheet image <see cref="Canvas.DrawSprite"/> draws from, or null if the app draws no sprites.</summary>
    public SpriteSheetLayout? SpriteSheet { get; }

    /// <summary>Draws the parts that rarely change. Called again only after <see cref="Invalidation.Static"/>.</summary>
    protected abstract void DrawStatic(Canvas canvas);

    /// <summary>Draws the parts that change, on top of the static frame.</summary>
    protected abstract void Draw(Canvas canvas);

    /// <summary>Handles a click at a position in frame coordinates.</summary>
    /// <returns>The frames that need to be drawn again.</returns>
    protected virtual Invalidation OnClick(float x, float y) => Invalidation.None;

    public Frame RenderStatic()
    {
        var canvas = new Canvas(FrameWidth, FrameHeight);
        DrawStatic(canvas);
        return canvas.Build();
    }

    public Frame RenderDynamic()
    {
        var canvas = new Canvas(FrameWidth, FrameHeight);
        Draw(canvas);
        return canvas.Build();
    }

    public Invalidation Click(float x, float y) => OnClick(x, y);
}
