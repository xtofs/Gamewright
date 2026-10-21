namespace Gamewright;

using System.Runtime.InteropServices;

/// <summary>How text is drawn: fill colour and font. Sent to the renderer as 8 bytes.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct TextStyle(uint ColorRgba, int Font);
