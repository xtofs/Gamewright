namespace Gamewright.HexBoard.Graphics;

using System.Numerics;
using Gamewright.HexBoard;

/// <summary>
/// Maps a circular hex grid of pointy-top hexagons onto a rectangular screen area.
/// The grid is scaled to the largest size that fits the area and centered in it.
/// All positions are in screen units with y pointing down.
/// </summary>
/// <remarks>
/// With circumradius s, the center of (q, r, s) is s * (√3/2 (q - r), -3/2 s), see MATH.md.
/// +Q points east and +S points north, matching <see cref="GridCornerDirection"/>.
/// </remarks>
public sealed class HexLayout(int radius)
{
    private static readonly float Sqrt3 = MathF.Sqrt(3);

    // corners of a hexagon with circumradius 1, starting at the bottom vertex
    private static readonly Vector2[] UnitCorners = [..
        from i in Enumerable.Range(0, 6)
        let angle = MathF.PI / 3 * i
        select new Vector2(MathF.Sin(angle), MathF.Cos(angle))];

    /// <summary>The radius of the grid, in hexagons.</summary>
    public int Radius { get; } = radius;

    /// <summary>The circumradius of each hexagon in screen units.</summary>
    public float HexagonRadius { get; private set; }

    /// <summary>The screen position of the center of hexagon (0, 0, 0).</summary>
    public Vector2 Origin { get; private set; }

    /// <summary>The side length of the largest axis-aligned square inside a hexagon.</summary>
    public float InscribedSquareSide => HexagonRadius * (3 - Sqrt3);

    /// <summary>The width divided by the height of a grid with the given radius.</summary>
    public static float AspectRatio(int radius) => Sqrt3 * (2 * radius + 1) / (3 * radius + 2);

    /// <summary>
    /// Scales and centers the grid so it fills an area of <paramref name="areaSize"/> at (0, 0).
    /// </summary>
    public void Update(Vector2 areaSize) => Update(Vector2.Zero, areaSize);

    /// <summary>
    /// Scales and centers the grid so it fills the area at <paramref name="areaPosition"/>
    /// with <paramref name="areaSize"/>.
    /// </summary>
    public void Update(Vector2 areaPosition, Vector2 areaSize)
    {
        // the grid is √3 (2R+1) hexagon radii wide and 3R+2 hexagon radii high
        var width = Sqrt3 * (2 * Radius + 1);
        var height = 3 * Radius + 2;

        HexagonRadius = MathF.Max(0, MathF.Min(areaSize.X / width, areaSize.Y / height));
        Origin = areaPosition + areaSize / 2;
    }

    /// <summary>Returns all hex coordinates in the grid.</summary>
    public IEnumerable<Coordinate> GetGrid() => CubeMath.CubeHexRegion(Radius);

    /// <summary>Returns the screen position of the center of <paramref name="hex"/>.</summary>
    public Vector2 GetCenter(Coordinate hex) =>
        Origin + HexagonRadius * new Vector2(Sqrt3 / 2 * (hex.Q - hex.R), -1.5f * hex.S);

    /// <summary>Returns the six screen-space corners of <paramref name="hex"/>.</summary>
    public HexCorners GetHexCorners(Coordinate hex)
    {
        var center = GetCenter(hex);
        var corners = new HexCorners();
        for (var i = 0; i < 6; i++)
        {
            corners[i] = center + UnitCorners[i] * HexagonRadius;
        }
        return corners;
    }

    /// <summary>Returns the largest axis-aligned square inside <paramref name="hex"/>.</summary>
    public (Vector2 Position, Vector2 Size) GetInscribedSquare(Coordinate hex)
    {
        var side = InscribedSquareSide;
        return (GetCenter(hex) - new Vector2(side / 2), new Vector2(side));
    }

    /// <summary>
    /// Returns true if <paramref name="position"/> lies in a hexagon of the grid,
    /// and sets <paramref name="hex"/> to that hexagon.
    /// </summary>
    public bool TryGetHex(Vector2 position, out Coordinate hex)
    {
        if (HexagonRadius < 1e-6f)
        {
            hex = default;
            return false;
        }

        // inverse of GetCenter
        var p = (position - Origin) / HexagonRadius;
        var q = p.X / Sqrt3 + p.Y / 3;
        var r = -p.X / Sqrt3 + p.Y / 3;
        var s = -2 * p.Y / 3;

        hex = CubeRound(q, r, s);
        return hex.IsWithin(Radius);
    }

    /// <summary>Rounds fractional cube coordinates to the cube coordinate of the containing hexagon.</summary>
    internal static Coordinate CubeRound(float q, float r, float s)
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

        return new Coordinate(rq, rr, rs);
    }
}
