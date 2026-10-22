namespace Gamewright.HexagonalGrid;

using System.Numerics;
using System.Runtime.InteropServices;

/// <summary>
/// Provides geometric functions for working with hexagonal grids, such as calculating the positions of hexagon vertices and centers.
/// All positions are relative to the center of hexagon (0, 0, 0) and scale with the hexagon's circumradius.
/// </summary>
static class HexGeometry
{
    private static readonly float Sqrt3 = MathF.Sqrt(3);

    private static readonly Hexagon UnitFlatTop = CreateUnitHexagon(HexOrientation.FlatTop);
    private static readonly Hexagon UnitPointyTop = CreateUnitHexagon(HexOrientation.PointyTop);

    private static Hexagon CreateUnitHexagon(HexOrientation orientation)
    {
        var hex = new Hexagon();
        var angleIncrement = MathF.PI / 3;
        for (int i = 0; i < 6; i++)
        {
            var angle = (orientation.StartAngle + i) * angleIncrement;
            hex[i] = new Point(MathF.Cos(angle), MathF.Sin(angle));
        }
        return hex;
    }

    /// <summary>
    /// The six corners of a hexagon. Returned by value: an inline array converts implicitly to
    /// <see cref="ReadOnlySpan{Point}"/>, and the compiler then checks that the span does not outlive the local.
    /// </summary>
    internal static Hexagon Corners(Point center, float hexagonCircumRadius, HexOrientation orientation)
    {
        var unit = orientation.IsPointyTop ? UnitPointyTop : UnitFlatTop;
        var result = new Hexagon();

        // Six points are three Vector4s of two points each: scale and translate in three steps.
        ReadOnlySpan<Point> unitPoints = unit;
        ReadOnlySpan<Vector4> from = MemoryMarshal.Cast<Point, Vector4>(unitPoints);
        Span<Point> resultPoints = result;
        Span<Vector4> to = MemoryMarshal.Cast<Point, Vector4>(resultPoints);
        var offset = new Vector4(center.X, center.Y, center.X, center.Y);
        for (int i = 0; i < to.Length; i++)
        {
            to[i] = from[i] * hexagonCircumRadius + offset;
        }
        return result;
    }

    /// <summary>Center of <paramref name="hex"/>, as a displacement from the center of hexagon (0, 0, 0).</summary>
    internal static Displacement OffsetFromCenter(Hex hex, float hexagonCircumradius, HexOrientation orientation) =>
        (Displacement)(Vector2.Transform(new Vector2(hex.Q, hex.R), orientation.Forward) * hexagonCircumradius);

    /// <summary>
    /// The hexagon containing <paramref name="offset"/> (a displacement from the center of hexagon (0, 0, 0)).
    /// Inverse of <see cref="OffsetFromCenter"/> followed by <see cref="HexMath.Round"/>; not limited to any board.
    /// </summary>
    internal static Hex FromPoint(Displacement offset, float hexagonCircumRadius, HexOrientation orientation)
    {
        var axial = Vector2.Transform((Vector2)(offset / hexagonCircumRadius), orientation.Inverse);
        return HexMath.Round(axial.X, axial.Y, -axial.X - axial.Y);
    }

    /// <summary>
    /// The largest hexagon circumradius at which a whole board of the given radius fits into <paramref name="available"/>.
    /// With m = radius − 1 outer rings, the board spans (1.5·m + 1)·size along the axis where cells
    /// have a corner and √3·(m + ½)·size along the axis where they have a flat side.
    /// </summary>
    internal static float FitCircumradius(Size available, int radius, HexOrientation orientation)
    {
        var m = radius - 1;
        var cornerAxis = 1.5f * m + 1;           // half extent in units of the circumradius
        var flatAxis = Sqrt3 * (m + 0.5f);
        var (halfW, halfH) = orientation.IsPointyTop ? (flatAxis, cornerAxis) : (cornerAxis, flatAxis);
        return MathF.Max(0, MathF.Min(available.Width / 2 / halfW, available.Height / 2 / halfH));
    }
}
