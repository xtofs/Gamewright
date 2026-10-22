namespace Gamewright;

/// <summary>A path operation. How many floats of geometry each takes and what it does: <c>docs/frame-spec.md</c> section 4.</summary>
public enum PathOpcode : byte
{
    MoveTo,
    LineTo,
    QuadTo,
    CubicTo,
    ArcTo,
    ClosePath
}
