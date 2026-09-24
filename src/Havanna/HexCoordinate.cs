namespace Havanna;

using System.Numerics;
using System.Diagnostics;

public record struct HexCoordinate(int X, int Y, int Z)
{
    // the basis vector pair for flat top:
    public static readonly (Vector3 U, Vector3 V) Basis = (new Vector3(1, -1, 0) / MathF.Sqrt(2), new Vector3(1, 1, -2) / MathF.Sqrt(6));

    public readonly Vector2 Center
    {
        get
        {
            Debug.Assert(this.X + this.Y + this.Z == 0, "Invalid hex coordinate");

            var p = new Vector3(this.X, this.Y, this.Z);
            var point = new Vector2(Vector3.Dot(p, Basis.U), Vector3.Dot(p, Basis.V));
            return point;
        }
    }


    public static IEnumerable<HexCoordinate> GetGrid(int n)
    {
        for (var x = -n; x <= n; x++)
        {
            for (var y = -n; y <= n; y++)
            {
                for (var z = -n; z <= n; z++)
                {
                    if (x + y + z == 0)
                    {
                        yield return new HexCoordinate(x, y, z);
                    }
                }
            }
        }
    }

    internal static HexCoordinate CubeRound(float x, float y, float z)
    {
        var rx = (int)MathF.Round(x);
        var ry = (int)MathF.Round(y);
        var rz = (int)MathF.Round(z);

        var dx = MathF.Abs(rx - x);
        var dy = MathF.Abs(ry - y);
        var dz = MathF.Abs(rz - z);

        if (dx > dy && dx > dz)
        {
            rx = -ry - rz;
        }
        else if (dy > dz)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new HexCoordinate(rx, ry, rz);
    }

}
