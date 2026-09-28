namespace Gamewright.HexBoard;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// A hexagon on a circular hexagonal board, located relative to the <see cref="Center"/> hexagon.
/// </summary>
/// <remarks>
/// Stored as cube coordinates with Q + R + S = 0: +Q points east and +S points north.
/// See <see cref="Direction"/> and <see cref="Offset"/> for Coordinate arithmetic.
/// </remarks>
public readonly record struct Coordinate
{
    /// <summary>The center hexagon of the board.</summary>
    public static Coordinate Center { get; } = new(0, 0, 0);

    public Coordinate(int q, int r, int s)
    {
        ArgumentOutOfRangeException.ThrowIfNotZero(q + r + s, "The sum of q, r, and s must be 0.");
        Q = (short)q;
        R = (short)r;
    }

    public readonly short Q { get; }
    public readonly short R { get; }

    // S is derived from the constraint Q + R + S = 0
    public readonly short S => (short)(-(Q + R));

    public static implicit operator (int Q, int R, int S)(Coordinate value)
    {
        return (value.Q, value.R, value.S);
    }

    public static implicit operator Coordinate((int Q, int R, int S) value)
    {
        return new Coordinate(value.Q, value.R, value.S);
    }

    public void Deconstruct(out int q, out int r, out int s)
    {
        q = this.Q;
        r = this.R;
        s = this.S;
    }

    /// <summary>
    /// Returns the ring number of the hexagon, its distance from the center in a circular hexagonal grid.
    /// </summary>
    /// <remarks>
    /// This is calculates from the maximum of the absolute values of the cube coordinates.
    /// which is the same as (|q| + |r| + |s|) / 2 because of the constraint q + r + s = 0.
    /// </remarks>
    public int Ring() => CubeMath.RingOfCube(Q, R, S);

    /// <summary>
    /// Determines if the hexagon lies within a circular hexagonal grid with the given radius.
    /// </summary>
    public bool IsWithin(int radius) => Ring() <= radius;

    /// <summary>
    /// Returns the index of the hexagon in a list or hexes of a circular hexagonal grid.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public readonly int GetIndex()
    {
        var (q, r, s) = this;
        return CubeMath.IndexFromCube(q, r, s);
    }

    public static Coordinate FromIndex(int index)
    {
        CubeMath.CubeFromIndex(index, out var q, out var r, out var s);
        return new Coordinate(q, r, s);
    }

    /// <summary>
    /// Determines if the hexagon is a grid corner of the grid with the given radius.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="corner"></param>
    /// <returns></returns>
    public bool IsCorner(int radius, [MaybeNullWhen(false)] out GridCornerDirection corner)
    {
        // corners are determined by which components have 
        // absolute value equal to the radius and their signs.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        // convert the coordinates to -1, 0, or 1 based on whether they are on the radius boundary
        // this allows to compare to constants in the switch statement below.
        var q = Q.Abs() == radius ? Q.Sgn() : 0;
        var r = R.Abs() == radius ? R.Sgn() : 0;
        var s = S.Abs() == radius ? S.Sgn() : 0;
        // for corners, and only for corners, q, r, s is not a permutation of 1, 0, -1 

        switch (q, r, s)
        {
            case (-1, 0, 1):
                corner = GridCornerDirection.NorthWest;
                return true;
            case (0, -1, 1):
                corner = GridCornerDirection.NorthEast;
                return true;
            case (1, -1, 0):
                corner = GridCornerDirection.East;
                return true;
            case (1, 0, -1):
                corner = GridCornerDirection.SouthEast;
                return true;
            case (0, 1, -1):
                corner = GridCornerDirection.SouthWest;
                return true;
            case (-1, 1, 0):
                corner = GridCornerDirection.West;
                return true;
            default:
                corner = (GridCornerDirection)(-1);
                return false;
        }
    }

    /// <summary>
    /// Determines if the hexagon is on an edge of the hexagonal grid with the given radius.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="edge"></param>
    /// <returns></returns>
    internal bool IsEdge(int radius, [MaybeNullWhen(false)] out GridEdgeDirection edge)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        // the edge is determined by the sign of the component that has 
        // the absolute value equal to the radius.

        var q = this.Q.Abs() == radius ? this.Q.Sgn() : 0;
        var r = this.R.Abs() == radius ? this.R.Sgn() : 0;
        var s = this.S.Abs() == radius ? this.S.Sgn() : 0;
        switch (q, r, s)
        {
            case (-1, 0, 0):
                edge = GridEdgeDirection.NorthWest;
                return true;
            case (1, 0, 0):
                edge = GridEdgeDirection.SouthEast;
                return true;
            case (0, -1, 0):
                edge = GridEdgeDirection.NorthEast;
                return true;
            case (0, 1, 0):
                edge = GridEdgeDirection.SouthWest;
                return true;
            case (0, 0, 1):
                edge = GridEdgeDirection.North;
                return true;
            case (0, 0, -1):
                edge = GridEdgeDirection.South;
                return true;
            default:
                edge = (GridEdgeDirection)(-1);
                return false;
        }
    }

}
