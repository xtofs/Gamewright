namespace Gamewright;

[Flags]
public enum DrawCommandFlags : ushort
{
    None = 0,
    Fill = 1,
    Stroke = 2,
    Closed = 4,
    AlignCenter = 8,
    AlignEnd = 16
}
