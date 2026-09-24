namespace Havanna;

using System.Numerics;

public sealed class HexLayout
{
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
        var farDown = new HexCoordinate(0, _n + 1, -(_n + 1)).Center.Y;
        var farRight = new HexCoordinate(_n + 1, -(_n + 1), 0).Center.X;

        // hexRadius must satisfy: farRight * hexRadius <= frameSize.X / 2
        // (and the same for Y), because frameCenter == frameSize / 2.
        _hexRadius = MathF.Min(frameSize.X / (2 * farRight), frameSize.Y / (2 * farDown));
    }

    public float HexRadius => _hexRadius;

    /// <summary>Returns all hex coordinates in the grid.</summary>
    public IEnumerable<HexCoordinate> GetGrid() => HexCoordinate.GetGrid(_n);

    /// <summary>Returns the screen-space center of <paramref name="hex"/> in framebuffer pixels.</summary>
    public Vector2 GetCenter(HexCoordinate hex) =>
        hex.Center * _hexRadius + _frameCenter;

    /// <summary>Returns the 6 screen-space vertices of the hexagon for <paramref name="hex"/>.</summary>
    public Vector2[] GetHexagon(HexCoordinate hex)
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
    public bool TryGetHex(Vector2 screenPos, out HexCoordinate hex)
    {
        if (_hexRadius < 1e-6f)
        {
            hex = default;
            return false;
        }

        var point = (screenPos - _frameCenter) / _hexRadius;
        var (U, V) = HexCoordinate.Basis;
        float fx = point.X * U.X + point.Y * V.X;
        float fy = point.X * U.Y + point.Y * V.Y;
        float fz = point.X * U.Z + point.Y * V.Z;
        hex = HexCoordinate.CubeRound(fx, fy, fz);
        return hex.X >= -_n && hex.X <= _n
            && hex.Y >= -_n && hex.Y <= _n
            && hex.Z >= -_n && hex.Z <= _n;
    }
}
