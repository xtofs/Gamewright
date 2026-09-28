namespace Gamewright.HexBoard;

/// <summary>
/// provides utility methods for working with cube coordinates in a hexagonal grid.
/// Contains methods to convert between cube coordinates and linear indices, and to determine rings in the hexagonal grid.
/// </summary>
public static class CubeMath
{
    public static int IndexFromCube(int q, int r, int s)
    {
        var k = CubeMath.RingOfCube(q, r, s);

        // determine grid edge "index" and offset along the edge
        // edge are numbered clockwise starting with the edge that starts on the top right
        // and the last hex on the edge is excluded
        if (k == 0) { return 0; }

        var (e, t) = 0 switch
        {
            _ when q == +k && r != 0 => (0, -s),
            _ when s == -k && q != 0 => (1, r),
            _ when r == +k && s != 0 => (2, -q),
            _ when q == -k && r != 0 => (3, s),
            _ when s == +k && q != 0 => (4, -r),
            _ when r == -k && s != 0 => (5, q),
            _ => throw new InvalidDataException($"Internal error: mismatch of cube coordinate {(q, r, s)} and calculated ring {k}")
        };

        return 1 + 3 * k * (k - 1) + e * k + t;
    }

    public static void CubeFromIndex(int ix, out int q, out int r, out int s)
    {
        if (ix == 0) { q = r = s = 0; return; }

        var k = RingOfIndex(ix);
        var firstIndexInRing = 1 + 3 * k * (k - 1);
        var indexInRing = ix - firstIndexInRing;
        var edge = indexInRing / k;
        var i = indexInRing % k; // index within the edge

        switch (edge)
        {
            case 0: q = k; s = -i; r = -(q + s); break;
            case 1: s = -k; r = i; q = -(r + s); break;
            case 2: r = k; q = -i; s = -(q + r); break;

            case 3: q = -k; s = i; r = -(q + s); break;
            case 4: s = k; r = -i; q = -(r + s); break;
            case 5: r = -k; q = i; s = -(q + r); break;
            // case 3: q = -k; s = i; r = -(q + s); break;
            // case 4: s = -k; r = i; q = -(r + s); break;

            // case 0: q = -k + i; r = k; s = -i; break;
            // case 1: q = i; r = k - i; s = -k; break;
            // case 2: q = k; r = -i; s = -k + i; break;
            // case 3: q = -i + k; r = -k; s = i; break;
            // case 4: q = -i; r = -k + i; s = k; break;
            // case 5: q = -k; r = i; s = k - indexInEdge; break;
            // default: throw new InvalidDataException();
            default: q = 0; r = 0; s = 0; break;
        }
    }

    #region Ring related functions

    public static int RingOfCube(int q, int r, int s)
    {
        return Math.Max(q.Abs(), r.Abs(), s.Abs());
    }

    public static int NumberOfHexagonsInRing(int k)
    {
        if (k == 0) { return 1; }
        return 6 * k;
    }

    public static int NumberOfHexagonsInGrid(int k)
    {
        return 1 + 3 * k * (k + 1);
    }
    public static int RingOfIndex(int ix)
    {
        //             var k = 1; // find the ring
        //             while (3 * k * k + 3 * k + 1 <= ix) { k++; }
        //             return k;

        if (ix < 7) { return 1; }                        // the loop returns 1 for ix < 7, including ix ≤ 0
        var n = 12 * ix - 3;
        var r = Math.ISqrt(n);
        return (3 + r) / 6;
    }

    #endregion


    /// <summary>
    /// Enumerates all hexagons within a hexagonal grid of radius k, centered at the origin,
    /// in order of increasing index
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    public static IEnumerable<(Coordinate, int)> EnumerateGridCoords(int k)
    {
        var n = NumberOfHexagonsInGrid(k);
        for (var i = 0; i < n; i++)
        {
            var cube = Coordinate.FromIndex(i);
            yield return (cube, i);
        }
    }

    /// <summary>
    /// Returns all hexagons within a hexagonal grid of radius R, centered at the origin.
    /// that are all hexagons with max(|q|,|r|,|s|) <= R  (count = 3R²+3R+1)
    /// </summary>
    /// <param name="R"></param>
    /// <returns></returns>
    public static IEnumerable<Coordinate> CubeHexRegion(int R)
    {
        for (var q = -R; q <= R; q++)
        {
            for (var r = Math.Max(-R, -q - R); r <= Math.Min(R, -q + R); r++)
            {
                var s = -q - r;
                yield return new Coordinate(q, r, s);
            }
        }
    }

}
