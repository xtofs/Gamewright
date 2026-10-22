namespace Gamewright;

/// <summary>
/// A recorded drawing. A command's <c>StyleIndex</c> indexes <see cref="Paints"/> for a path and
/// <see cref="TextStyles"/> for text; an image command has no style but names an entry of
/// <see cref="Images"/>. The layout of every part and what a
/// renderer must do with it are specified in <c>docs/frame-spec.md</c>.
/// </summary>
public sealed record Frame(
    DrawCommand[] Commands,
    byte[] PathData,
    float[] Geometry,
    Paint[] Paints,
    TextStyle[] TextStyles,
    string[] Strings,
    string[] Images);
