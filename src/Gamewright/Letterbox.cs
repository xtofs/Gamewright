namespace Gamewright;

/// <summary>
/// How a frame of a fixed size is placed in a box of any shape: one uniform scale, centered, so the
/// frame never distorts and the spare room is split evenly on one axis. Converts between box
/// coordinates (pixels of the window or canvas) and frame coordinates.
/// </summary>
public readonly record struct Letterbox(float Scale, float OffsetX, float OffsetY)
{
    public static Letterbox Fit(float frameWidth, float frameHeight, float boxWidth, float boxHeight)
    {
        if (!(frameWidth > 0) || !(frameHeight > 0))
            throw new ArgumentOutOfRangeException(nameof(frameWidth), "Frame dimensions must be positive.");
        if (!(boxWidth > 0) || !(boxHeight > 0))
            return default;

        var scale = Math.Min(boxWidth / frameWidth, boxHeight / frameHeight);
        return new Letterbox(scale, (boxWidth - frameWidth * scale) / 2, (boxHeight - frameHeight * scale) / 2);
    }

    /// <summary>True for an empty box, where nothing can be drawn or hit.</summary>
    public bool IsEmpty => !(Scale > 0);

    public (float X, float Y) ToFrame(float boxX, float boxY)
    {
        RequireNotEmpty();
        return ((boxX - OffsetX) / Scale, (boxY - OffsetY) / Scale);
    }

    public (float X, float Y) FromFrame(float frameX, float frameY)
    {
        RequireNotEmpty();
        return (frameX * Scale + OffsetX, frameY * Scale + OffsetY);
    }

    private void RequireNotEmpty()
    {
        if (IsEmpty)
            throw new InvalidOperationException("The box is empty.");
    }
}
