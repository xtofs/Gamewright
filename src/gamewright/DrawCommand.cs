namespace Gamewright;

public readonly record struct DrawCommand(
    DrawCommandType Type,
    ushort StyleIndex,
    ushort Flags,
    int GeometryOffset,
    int GeometryLength,
    int PathDataOffset,
    int PathDataLength,
    int Data);
