namespace Havanna;

using System.Numerics;
using Gamewright.HexBoard;

public sealed class HexLayout
{
    // the orthonormal basis of the plane perpendicular to (1,1,1), see MATH.md.
    // Projecting cube coordinates onto it yields pointy-top hexagons.
    private static readonly (Vector3 U, Vector3 V) Basis = (new Vector3(1, -1, 0) / MathF.Sqrt(2), new Vector3(1, 1, -2) / MathF.Sqrt(6));

    private readonly int _n;
    private Vector2 _frameCenter;
    private float _hexRadius;

    private static readonly Vector2[] UnitHexagon = Enumerable.Range(0, 6)
        .Select(i =>
        {
            var angle = MathF.PI / 3 * i;
            return new Vector2(MathF.Sin(angle), MathF.Cos(angle));
        })
        .ToArray();

    public HexLayout(int n)
    {
        _n = n;
    }

    /// <summary>
    /// Recomputes the layout from the current framebuffer size.
    /// Call once per frame in OnRender (and on Resize).
    /// </summary>
    public void Update(Vector2 frameSize)
    {
        _frameCenter = frameSize / 2;

        // The farthest hex center in each axis for a grid of radius _n.
        // _n+1 is used so that the outermost hexagon's body stays inside the frame,
        // not just its center.
        var farDown = Project(new CubeCoord(0, _n + 1, -(_n + 1))).Y;
        var farRight = Project(new CubeCoord(_n + 1, -(_n + 1), 0)).X;

        // hexRadius must satisfy: farRight * hexRadius <= frameSize.X / 2
        // (and the same for Y), because frameCenter == frameSize / 2.
        _hexRadius = MathF.Min(frameSize.X / (2 * farRight), frameSize.Y / (2 * farDown));
    }

    public float HexRadius => _hexRadius;

    /// <summary>Returns all hex coordinates in the grid.</summary>
    public IEnumerable<CubeCoord> GetGrid() => CubeMath.CubeHexRegion(_n);

    /// <summary>Returns the screen-space center of <paramref name="hex"/> in framebuffer pixels.</summary>
    public Vector2 GetCenter(CubeCoord hex) =>
        Project(hex) * _hexRadius + _frameCenter;

    /// <summary>Returns the 6 screen-space vertices of the hexagon for <paramref name="hex"/>.</summary>
    public Vector2[] GetHexagon(CubeCoord hex)
    {
        var center = GetCenter(hex);
        var radius = _hexRadius * MathF.Sqrt(6) / 3;
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            points[i] = center + UnitHexagon[i] * radius;
        }
        return points;
    }

    /// <summary>
    /// Returns true if <paramref name="screenPos"/> maps to a hex within the grid,
    /// and sets <paramref name="hex"/> to that hex coordinate.
    /// </summary>
    public bool TryGetHex(Vector2 screenPos, out CubeCoord hex)
    {
        if (_hexRadius < 1e-6f)
        {
            hex = default;
            return false;
        }

        var point = (screenPos - _frameCenter) / _hexRadius;
        var (U, V) = Basis;
        float fq = point.X * U.X + point.Y * V.X;
        float fr = point.X * U.Y + point.Y * V.Y;
        float fs = point.X * U.Z + point.Y * V.Z;
        hex = CubeRound(fq, fr, fs);
        return hex.IsWithin(_n);
    }

    /// <summary>Projects <paramref name="hex"/> onto the plane, in units of the layout scale.</summary>
    private static Vector2 Project(CubeCoord hex)
    {
        var p = new Vector3(hex.Q, hex.R, hex.S);
        return new Vector2(Vector3.Dot(p, Basis.U), Vector3.Dot(p, Basis.V));
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
}
