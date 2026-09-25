namespace Havanna;

using System.Numerics;
using Gamewright;
using Gamewright.HexBoard;

/// <summary>
/// Represents the layout of a hexagonal grid with hexagons laid out around a central hexagon.
/// Provides methods to convert between hex coordinates and screen-space positions, and to compute hex geometry.
/// The layout is defined by the grid radius and the hex radius, which is computed based on the framebuffer size.
/// </summary>
public sealed class HexLayout(int radius)
{
    /// <summary>
    /// The radius of the hex grid, in hex units. Determines how many hexes are included in the layout.
    ///  Gets updated whenever the layout is recomputed.
    /// </summary>
    private readonly int _gridSize = radius;


    /// <summary>
    /// The circumradius of each hexagon in screen-space units.
    /// Gets updated whenever the layout is recomputed.
    /// </summary>
    private float _hexagonRadius;

    private float _coordinateScale;

    /// <summary>
    /// The center of the framebuffer, in screen-space units.
    /// Gets updated whenever the layout is recomputed.
    /// </summary>
    private Vector2 _frameCenter;


    /// <summary>
    /// Recomputes the layout from the current framebuffer size.
    /// Call once per frame in OnRender (and on Resize).
    /// </summary>
    public void Update(Vector2 frameSize)
    {
        _frameCenter = frameSize / 2;

        // The farthest hex center in each axis for a grid of radius _gridRadius.
        // _n+1 is used so that the outermost hexagon's body stays inside the frame,
        // not just its center.
        var farBottom = HexGeometry.Center(new CubeCoord(0, _gridSize + 1, -(_gridSize + 1))).Y;
        var farRight = HexGeometry.Center(new CubeCoord(_gridSize + 1, -(_gridSize + 1), 0)).X;

        // _coordinateScale must satisfy: farRight * _hexagonRadius <= frameSize.X / 2 and farBottom * _hexagonRadius <= frameSize.Y / 2
        _coordinateScale = MathF.Min(frameSize.X / (2 * farRight), frameSize.Y / (2 * farBottom));
        _hexagonRadius = _coordinateScale * MathF.Sqrt(6) / 3;
    }

    public float HexagonRadius => _hexagonRadius;

    /// <summary>Returns all hex coordinates in the grid.</summary>
    public IEnumerable<CubeCoord> GetGrid() => CubeMath.CubeHexRegion(_gridSize);

    private static readonly Vector2[] UnitHexagon = [..
        from i in Enumerable.Range(0, 6)
        let angle = MathF.PI / 3 * i
        select new Vector2(MathF.Sin(angle), MathF.Cos(angle))];


    /// <summary>
    /// Returns the 6 screen-space vertices of the hexagon for <paramref name="hex"/>.
    /// </summary>
    public Vector2[] GetHexagon(CubeCoord hex)
    {
        var center = GetCenter(hex);
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            points[i] = center + UnitHexagon[i] * _hexagonRadius;
        }
        return points;
    }

    /// <summary>Returns the screen-space center of <paramref name="hex"/> in framebuffer pixels.</summary>
    public Vector2 GetCenter(CubeCoord hex) =>
        HexGeometry.Center(hex) * _coordinateScale + _frameCenter;


    /// <summary>Returns the inscribed square of <paramref name="hex"/> in framebuffer pixels.</summary>
    public Rect GetInscribedSquare(CubeCoord hex)
    {
        var square = HexGeometry.GetInscribedSquare(hex);
        var position = square.Position * _coordinateScale + _frameCenter;
        var size = square.Size * _coordinateScale;
        return new Rect(position, size);
    }

    /// <summary>
    /// Returns true if <paramref name="screenPos"/> maps to a hex within the grid,
    /// and sets <paramref name="hex"/> to that hex coordinate.
    /// </summary>
    public bool TryGetHex(Vector2 screenPos, out CubeCoord hex)
    {
        if (_coordinateScale < 1e-6f)
        {
            hex = default;
            return false;
        }

        var point = (screenPos - _frameCenter) / _coordinateScale;

        hex = HexGeometry.GetHexagon(point);

        return hex.IsWithin(_gridSize);
    }
}
