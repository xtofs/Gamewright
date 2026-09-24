namespace HexLib;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// hexagonal grid cube coordinates
/// constraint Q + R + S = 0
/// </summary>
public readonly record struct CubeCoord
{
    public CubeCoord(int q, int r, int s)
    {
        ArgumentOutOfRangeException.ThrowIfNotZero(q + r + s, "The sum of q, r, and s must be 0.");
        Q = (short)q;
        R = (short)r;
    }

    public readonly short Q { get; }
    public readonly short R { get; }

    // S is derived from the constraint Q + R + S = 0
    public readonly short S => (short)(-(Q + R));

    public static implicit operator (int Q, int R, int S)(CubeCoord value)
    {
        return (value.Q, value.R, value.S);
    }

    public static implicit operator CubeCoord((int Q, int R, int S) value)
    {
        return new CubeCoord(value.Q, value.R, value.S);
    }

    public void Deconstruct(out int q, out int r, out int s)
    {
        q = this.Q;
        r = this.R;
        s = this.S;
    }

    /// <summary>
    /// Returns the ring number of the cube, the distance from the center in a circular hexagonal grid.
    /// </summary>
    /// <remarks>
    /// This is calculates from the maximum of the absolute values of the cube coordinates.
    /// which is the same as (|q| + |r| + |s|) / 2 because of the constraint q + r + s = 0.
    /// </remarks>
    public int Ring() => CubeMath.RingOfCube(Q, R, S);


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

    public static CubeCoord FromIndex(int index)
    {
        CubeMath.CubeFromIndex(index, out var q, out var r, out var s);
        return new CubeCoord(q, r, s);
    }

    /// <summary>
    /// Determines if the cube coordinate is a grid corner of the grid with the given radius.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="corner"></param>
    /// <returns></returns>
    public bool IsCorner(int radius, [MaybeNullWhen(false)] out CornerDirection corner)
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
                corner = CornerDirection.NorthWest;
                return true;
            case (0, -1, 1):
                corner = CornerDirection.NorthEast;
                return true;
            case (1, -1, 0):
                corner = CornerDirection.East;
                return true;
            case (1, 0, -1):
                corner = CornerDirection.SouthEast;
                return true;
            case (0, 1, -1):
                corner = CornerDirection.SouthWest;
                return true;
            case (-1, 1, 0):
                corner = CornerDirection.West;
                return true;
            default:
                corner = (CornerDirection)(-1);
                return false;
        }
    }

    /// <summary>
    /// Determines if the cube is on an edge of the hexagonal grid with the given radius.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="edge"></param>
    /// <returns></returns>
    internal bool IsEdge(int radius, [MaybeNullWhen(false)] out EdgeDirection edge)
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
                edge = EdgeDirection.NorthWest;
                return true;
            case (1, 0, 0):
                edge = EdgeDirection.SouthEast;
                return true;
            case (0, -1, 0):
                edge = EdgeDirection.NorthEast;
                return true;
            case (0, 1, 0):
                edge = EdgeDirection.SouthWest;
                return true;
            case (0, 0, 1):
                edge = EdgeDirection.North;
                return true;
            case (0, 0, -1):
                edge = EdgeDirection.South;
                return true;
            default:
                edge = (EdgeDirection)(-1);
                return false;
        }
    }

}
