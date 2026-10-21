namespace Gamewright;

public enum PathOpcode : byte
{
    MoveTo,
    LineTo,
    QuadTo,
    CubicTo,
    ArcTo,
    ClosePath
}
