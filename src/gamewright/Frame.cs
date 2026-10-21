namespace Gamewright;

/// <summary>
/// A recorded drawing. A command's <c>StyleIndex</c> indexes <see cref="Paints"/> for a path and
/// <see cref="TextStyles"/> for text; sprites have no style.
/// </summary>
public sealed record Frame(
    DrawCommand[] Commands,
    byte[] PathData,
    float[] Geometry,
    Paint[] Paints,
    TextStyle[] TextStyles,
    string[] Strings);
