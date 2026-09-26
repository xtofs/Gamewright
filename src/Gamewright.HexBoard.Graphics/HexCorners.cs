namespace Gamewright.HexBoard.Graphics;

using System.Numerics;
using System.Runtime.CompilerServices;

/// <summary>
/// The six corners of a hexagon as a fixed size value type, so they can be computed
/// every frame without allocating. Converts implicitly to <see cref="ReadOnlySpan{T}"/>.
/// </summary>
/// <remarks>
/// Corner 0 is the bottom vertex of the pointy-top hexagon, the others follow counterclockwise
/// on screen (y pointing down).
/// </remarks>
[InlineArray(6)]
public struct HexCorners
{
    private Vector2 _element0;
}
