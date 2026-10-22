namespace Gamewright.HexagonalGrid;


/// <summary>
/// Provides discrete mathematical functions for working with hexagonal grids and their cube coordinates.
/// </summary>
public static class HexMath
{
    /// <summary>
    /// Counts the total number of hexagons in a hexagonal grid of the given radius.
    /// </summary>
    /// <param name="radius">Cells along each edge of the board (Havannah's "base"); the rings are 0 … radius − 1.</param>
    /// <returns><c>3·radius·(radius − 1) + 1</c>, e.g. 169 for radius 8.</returns>
    public static int HexagonCount(int radius)
    {
        return 3 * radius * (radius - 1) + 1;
    }

    /// <summary>
    /// Determines the ring number of a hexagon in a hexagonal grid based on its cube coordinates.
    /// </summary>
    /// <param name="q"></param>
    /// <param name="r"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    public static int RingOfCube(Hex hex)
    {
        (int q, int r, int s) = hex;
        return Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(s)));
    }

    public static int RingOfCube(int q, int r, int s)
    {
        return Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(s)));
    }

    public static int RingOfIndex(int ix)
    {
        if (ix == 0) { return 0; }
        if (ix < 7) { return 1; }
        var n = 12 * ix - 3;
        var r = (int)Math.Sqrt(n);
        return (3 + r) / 6;
    }

    public static Hex HexFromIndex(int ix)
    {
        HexFromIndex(ix, out int q, out int r, out int s);
        return new Hex(q, r, s);
    }

    public static void HexFromIndex(int ix, out int q, out int r, out int s)
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

            // default: throw new InvalidDataException();
            default: q = 0; r = 0; s = 0; break;
        }
    }

    public static int IndexFromHex(Hex hex) => IndexFromHex(hex.Q, hex.R, hex.S);

    /// <summary>
    /// calculates the index of a hexagon in the hexagonal grid based on its cube coordinates.
    /// The index is stable and grows from the center of the grid outward and around each ring in a clockwise manner.
    /// </summary>
    /// <param name="q"></param>
    /// <param name="r"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static int IndexFromHex(int q, int r, int s)
    {
        var k = RingOfCube(q, r, s);

        // determine grid edge "index" and offset along the edge
        // edge are numbered clockwise starting with the edge that starts on the top right
        // and the last hex on the edge is excluded because it is the first hex of the next edge
        if (k == 0) { return 0; }

        // determine which edge of the ring the hexagon is on 
        // and its offset along that edge
        var (e, t) = 0 switch
        {
            _ when q == +k && r != 0 => (0, -s),
            _ when s == -k && q != 0 => (1, +r),
            _ when r == +k && s != 0 => (2, -q),
            _ when q == -k && r != 0 => (3, +s),
            _ when s == +k && q != 0 => (4, -r),
            _ when r == -k && s != 0 => (5, +q),
            _ => throw new InvalidDataException($"Internal error: mismatch of cube coordinate {(q, r, s)} and calculated ring {k}")
        };

        return 1 + 3 * k * (k - 1) + e * k + t;
    }

    /// <summary>
    /// Rounds fractional axial coordinates to the nearest hexagon. Rounds q, r and s independently, then
    /// recomputes the one with the largest rounding error from the other two, so q + r + s = 0 holds.
    /// </summary>
    public static Hex Round(float q, float r, float s)
    {
        if (q + r + s != 0) throw new ArgumentException("q + r + s must be 0");
        // float s = -q - r;
        int rq = (int)MathF.Round(q), rr = (int)MathF.Round(r), rs = (int)MathF.Round(s);
        float dq = MathF.Abs(rq - q), dr = MathF.Abs(rr - r), ds = MathF.Abs(rs - s);

        if (dq > dr && dq > ds) { rq = -rr - rs; }
        else if (dr > ds) { rr = -rq - rs; }
        else { rs = -rq - rr; }

        return new Hex(rq, rr, rs);
    }

    /// <summary>
    /// Enumerates all hexagons within a hexagonal grid of radius k, centered at the origin,
    /// in order of increasing index
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    public static IEnumerable<(Hex, int)> EnumerateHexes(int radius)
    {
        var n = HexagonCount(radius);
        for (var i = 0; i < n; i++)
        {
            var cube = Hex.FromIndex(i);
            yield return (cube, i);
        }
    }

    public static NeighborSet CreateNeighborSet(Hex hex, int boardRadius)
    {
        var neighbors = new NeighborSet();
        foreach (var neighbor in Neighbor.All)
        {
            var neighborHex = hex + neighbor.Direction;
            if (HexMath.IsOnBoard(neighborHex, boardRadius)) { neighbors = neighbors.Add(neighbor); }
        }

        return neighbors;
    }

    /// <summary>True when <paramref name="hex"/> lies on a board of the given radius (see <see cref="HexagonCount"/>).</summary>
    public static bool IsOnBoard(Hex hex, int boardRadius) => RingOfCube(hex) < boardRadius;
}
