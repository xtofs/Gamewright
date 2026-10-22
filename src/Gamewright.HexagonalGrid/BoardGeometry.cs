
namespace Gamewright.HexagonalGrid;

/// <summary>
/// Geometry of a Havannah board: the single source of truth for "where is this hexagon",
/// "what are its corners" and "which hexagon is under the pointer".
/// </summary>
/// <param name="Bounds">The rectangle the board is centered in and fitted to.</param>
/// <param name="Radius">Cells along each edge of the board (see <see cref="HexMath.HexagonCount"/>).</param>
/// <param name="IsBoardFlatSideUp">True: pointy-top cells, so the board has a flat side up. False: flat-top cells, corner up.</param>
public readonly record struct BoardGeometry(Rect Bounds, int Radius, bool IsBoardFlatSideUp = true)
{
    public readonly Point Center = Bounds.Center;

    internal readonly HexOrientation Orientation = IsBoardFlatSideUp ? HexOrientation.PointyTop : HexOrientation.FlatTop;

    public readonly float HexagonCircumRadius = HexGeometry.FitCircumradius(
        Bounds.Size, Radius, IsBoardFlatSideUp ? HexOrientation.PointyTop : HexOrientation.FlatTop);

    private readonly int HexesCount = HexMath.HexagonCount(Radius);

    public IEnumerable<Hex> GridHexes =>
        from int index in Enumerable.Range(0, HexesCount) select Hex.FromIndex(index);

    public Point HexCenter(Hex hex) => Center + HexGeometry.OffsetFromCenter(hex, HexagonCircumRadius, Orientation);

    /// <summary>The six corners of <paramref name="hex"/>, in drawing order (see <see cref="HexOrientation"/>).</summary>
    public Hexagon HexCorners(Hex hex) =>
        HexGeometry.Corners(HexCenter(hex), HexagonCircumRadius, Orientation);

    /// <summary>
    /// Every distinct edge between hexagons, once. An edge shared by two hexagons is yielded by the one with the
    /// lower index; board-border edges are yielded by their only hexagon.
    /// </summary>
    public IEnumerable<(Point A, Point B)> GridLines
    {
        get
        {
            var geometry = this;
            foreach (var hex in GridHexes)
            {
                var corners = geometry.HexCorners(hex);
                var neighbors = geometry.Orientation.EdgeNeighbors;
                for (var i = 0; i < 6; i++)
                {
                    var neighbor = hex + neighbors[i];
                    if (!HexMath.IsOnBoard(neighbor, Radius) || neighbor.Index > hex.Index)
                    {
                        yield return (corners[i], corners[(i + 1) % 6]);
                    }
                }
            }
        }
    }

    /// <summary><see cref="GridLines"/> flattened to endpoint pairs <c>[a0, b0, a1, b1, …]</c>, e.g. for <c>Canvas.StrokeSegments</c>.</summary>
    public Point[] GridLinePoints()
    {
        var points = new List<Point>(6 * HexesCount);
        foreach (var (a, b) in GridLines)
        {
            points.Add(a);
            points.Add(b);
        }
        return [.. points];
    }

    /// <summary>The hexagon under <paramref name="point"/>, if it is on the board.</summary>
    public bool TryGetHex(Point point, out Hex hex)
    {
        if (!(HexagonCircumRadius > 0))
        {
            hex = default;
            return false;   // nothing laid out yet (or zero-sized bounds)
        }

        var offset = point - Center;
        hex = HexGeometry.FromPoint(offset, HexagonCircumRadius, Orientation);
        return HexMath.IsOnBoard(hex, Radius);
    }
}
