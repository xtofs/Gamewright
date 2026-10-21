namespace Gamewright;

public sealed record Frame(
    DrawCommand[] Commands,
    byte[] PathData,
    float[] Geometry,
    Style[] Styles,
    string[] Strings);
