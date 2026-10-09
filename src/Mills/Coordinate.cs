namespace Mills;

public record struct Coordinate(int Ring, int Index)
{
    public override readonly string ToString() => $"({Ring}, {Index})";
    public static Coordinate Create(int ring, int index) => new Coordinate(ring, index);
    public static implicit operator Coordinate((int Ring, int Index) value) => new Coordinate(value.Ring, value.Index);
    public static implicit operator (int Ring, int Index)(Coordinate coord) => (coord.Ring, coord.Index);
    public readonly void Deconstruct(out int ring, out int index) => (ring, index) = (Ring, Index);


    /// <summary>
    /// Get the neighboring coordinates of the specified coordinate on the board.
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public readonly IReadOnlyList<Coordinate> Neighbors(Coordinate coord)
    {
        var neighbors = new List<Coordinate>(4);
        // clockwise neighbor
        neighbors.Add(coord with { Index = (coord.Index - 1) % 8 });

        // counter-clockwise neighbor
        neighbors.Add(coord with { Index = (coord.Index + 1) % 8 });

        if (coord.Index % 2 == 0)
        {
            // outer neighbor (previous ring)
            neighbors.Add(coord with { Ring = (coord.Ring - 1) % 3 });

            // lower neighbor (next ring)
            neighbors.Add(coord with { Ring = (coord.Ring + 1) % 3 });
        }

        return neighbors;
    }
}
