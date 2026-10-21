namespace Gamewright;

/// <summary>Which of an app's two frames no longer shows its current state and must be drawn again.</summary>
[Flags]
public enum Invalidation
{
    None = 0,

    /// <summary>The static frame (background, board, labels): drawn rarely and cached by the renderer.</summary>
    Static = 1,

    /// <summary>The dynamic frame (everything that changes): drawn on top of the static frame.</summary>
    Dynamic = 2,

    All = Static | Dynamic
}
