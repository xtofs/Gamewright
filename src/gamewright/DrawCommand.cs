namespace Gamewright;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DrawCommand(
    DrawCommandType Type,
    ushort StyleIndex,
    ushort Flags,
    int GeometryOffset,
    int GeometryLength,
    int PathDataOffset,
    int PathDataLength,
    int Data);
