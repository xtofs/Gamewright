namespace Gamewright;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Style(
    uint FillColor,
    uint StrokeColor,
    float StrokeWidth,
    int FontIndex);
