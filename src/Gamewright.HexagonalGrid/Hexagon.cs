namespace Gamewright.HexagonalGrid;

using System.Runtime.CompilerServices;

/// <summary>
/// The six corners of a hexagon, stored inline (no allocation). Converts implicitly to
/// <see cref="ReadOnlySpan{Point}"/>; the compiler checks that the span does not outlive the local.
/// </summary>
[InlineArray(6)]
public struct Hexagon
{
    private Point _element0;
}
