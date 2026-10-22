namespace Gamewright.HexagonalGrid;

using System.Numerics;

/// <summary>
/// How hexagons sit on screen. Pointy-top cells make a board with a flat side up, flat-top cells a board
/// with a corner up. Everything else about the layout (<see cref="HexOrientationExtensions"/>) follows from this.
/// </summary>
enum HexOrientation
{
    PointyTop,
    FlatTop,
}

/// <summary>
/// The axial → pixel matrix, its inverse and the angle of the first corner for each <see cref="HexOrientation"/>.
/// Screen y points down.
/// </summary>
/// <remarks>
/// <see cref="Matrix3x2"/> uses row vectors: (x, y) = (q, r) · M, so x = q·M11 + r·M21 and y = q·M12 + r·M22.
/// </remarks>
static class HexOrientationExtensions
{
    private static readonly float Sqrt3 = MathF.Sqrt(3);

    private static readonly Matrix3x2 PointyForward = new(Sqrt3, 0, Sqrt3 / 2, 1.5f, 0, 0);
    private static readonly Matrix3x2 FlatForward = new(1.5f, Sqrt3 / 2, 0, Sqrt3, 0, 0);

    private static readonly Matrix3x2 PointyInverse = Invert(PointyForward);
    private static readonly Matrix3x2 FlatInverse = Invert(FlatForward);

    // Offset of the neighbor across edge i, where edge i joins corner i and corner i + 1 (see HexGeometry.Corners).
    private static readonly HexOffset[] PointyEdgeNeighbors =
        [new(0, 1, -1), new(-1, 1, 0), new(-1, 0, 1), new(0, -1, 1), new(1, -1, 0), new(1, 0, -1)];
    private static readonly HexOffset[] FlatEdgeNeighbors =
        [new(1, 0, -1), new(0, 1, -1), new(-1, 1, 0), new(-1, 0, 1), new(0, -1, 1), new(1, -1, 0)];

    private static Matrix3x2 Invert(Matrix3x2 m) =>
        Matrix3x2.Invert(m, out var inverse) ? inverse : throw new InvalidOperationException("Hex layout matrix is not invertible.");

    extension(HexOrientation orientation)
    {
        public bool IsPointyTop => orientation == HexOrientation.PointyTop;

        /// <summary>Angle of the first corner, in multiples of 60°.</summary>
        public float StartAngle => orientation.IsPointyTop ? 0.5f : 0f;

        /// <summary>The neighbor offsets across each of the six edges; entry i is across the edge from corner i to corner i + 1.</summary>
        public IReadOnlyList<HexOffset> EdgeNeighbors => orientation.IsPointyTop ? PointyEdgeNeighbors : FlatEdgeNeighbors;

        /// <summary>Axial → pixel, for a hexagon of circumradius 1.</summary>
        public Matrix3x2 Forward => orientation.IsPointyTop ? PointyForward : FlatForward;

        /// <summary>Pixel → axial; the inverse of <see cref="Forward"/>.</summary>
        public Matrix3x2 Inverse => orientation.IsPointyTop ? PointyInverse : FlatInverse;
    }
}
