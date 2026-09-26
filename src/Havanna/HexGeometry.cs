namespace Havanna;

using System.Numerics;
using Gamewright.Graphics;
using Gamewright.HexBoard;

/// <summary>
/// Provides geometric utilities for working with hexagonal grids, including coordinate projection and rounding.
/// All methods operate in an idealized coordinate system with (0,0,0) at the center of the grid.
/// </summary>
public static class HexGeometry
{
    /// <summary>
    /// The circumradius of a unit hexagon in the idealized coordinate system.
    /// </summary>
    private static readonly float UnitHexagonCircumradius = MathF.Sqrt(6) / 3;

    /// <summary>
    /// The orthonormal basis of the plane perpendicular to the vector (1,1,1), used for projecting cube coordinates onto a 2D plane.
    /// See <see cref="Center"/> for how cube coordinates are projected onto the plane.
    /// </summary>
    private static readonly (Vector3 U, Vector3 V) BasisVectors =
        (new Vector3(1, -1, 0) / MathF.Sqrt(2), new Vector3(1, 1, -2) / MathF.Sqrt(6));


    /// <summary>
    /// Returns the unscaled 2D center of the hexagon corresponding to the given cube coordinate in the plane defined by <see cref="BasisVectors"/>.
    /// The (0,0,0) cube coordinate maps to the origin of this plane.
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static Vector2 Center(CubeCoord hex)
    {
        var p = new Vector3(hex.Q, hex.R, hex.S);
        return new Vector2(Vector3.Dot(p, BasisVectors.U), Vector3.Dot(p, BasisVectors.V));
    }

    /// <summary>
    /// Returns the largest axis-aligned square  in idealized grid coordinates that fits inside the hexagon at <paramref name="hex"/>.
    /// </summary>
    public static Rect GetInscribedSquare(CubeCoord hex)
    {
        var center = Center(hex);
        var side = UnitHexagonCircumradius * (3 - MathF.Sqrt(3));
        return new Rect(center.X - side / 2, center.Y - side / 2, side, side);
    }

    private static CubeCoord CubeRound(float q, float r, float s)
    {
        var rq = (int)MathF.Round(q);
        var rr = (int)MathF.Round(r);
        var rs = (int)MathF.Round(s);

        var dq = MathF.Abs(rq - q);
        var dr = MathF.Abs(rr - r);
        var ds = MathF.Abs(rs - s);

        if (dq > dr && dq > ds)
        {
            rq = -rr - rs;
        }
        else if (dr > ds)
        {
            rr = -rq - rs;
        }
        else
        {
            rs = -rq - rr;
        }

        return new CubeCoord(rq, rr, rs);
    }

    /// <summary>
    /// Returns the cube coordinate of the hexagon that contains the given point in the idealized 2D plane.
    /// Essentially the inverse of <see cref="Center"/> or <see cref="GetHexagon"/> 
    /// </summary>
    /// <param name="point">The point in the idealized 2D plane.</param>
    /// <returns>The cube coordinate of the hexagon containing the point.</returns>
    public static CubeCoord GetHexagon(Vector2 point)
    {
        var (U, V) = BasisVectors;
        var fq = point.X * U.X + point.Y * V.X;
        var fr = point.X * U.Y + point.Y * V.Y;
        var fs = point.X * U.Z + point.Y * V.Z;
        var hex = CubeRound(fq, fr, fs);
        return hex;
    }


}
