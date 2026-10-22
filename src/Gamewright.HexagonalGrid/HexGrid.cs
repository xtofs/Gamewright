namespace Gamewright.HexagonalGrid;


/// <summary>
/// A "circular" hexagonal grid representation made from pointy-top hexagons.
/// <see cref="Radius"/> is the number of cells along each edge of the board, so every hexagon is at most
/// <c>Radius - 1</c> steps away from the center hexagon. (1 is the degenerate case with just one hexagon.)
/// </summary>
/// <remarks>
/// The grid tracks occupancy itself, so <typeparamref name="T"/> only describes real pieces
/// and needs no "empty" value.
/// </remarks>
public class HexGrid<T> where T : struct
{
    private readonly Cell[] _cells;

    public int Radius { get; }

    public int Count { get; }

    public HexGrid(int radius)
    {
        Radius = radius;
        Count = HexMath.HexagonCount(radius);

        _cells = new Cell[Count];

        foreach (var (hex, index) in HexMath.EnumerateHexes(radius))
        {
            // initialize the cell at this index.             
            _cells[index] = Cell.Create(hex, radius);
        }
    }

    public bool TryGetCorner(Hex hex, out GridCorner corner)
    {
        return _cells[hex.Index].TryGetCorner(out corner);
    }

    public bool TryGetEdge(Hex hex, out GridEdge edge)
    {
        return _cells[hex.Index].TryGetEdge(out edge);
    }

    /// <summary>
    /// Represents a single cell within the hexagonal grid. Contains occupancy status, content, and neighbor information.
    /// Cell's default state is unoccupied with no content.
    /// </summary>
    public struct Cell
    {

        private Cell(bool Occupied, NeighborSet neighbors)
        {
            IsOccupied = Occupied;
            Neighbors = neighbors;
        }
        public bool IsOccupied { get; set; }

        public readonly NeighborSet Neighbors { get; }

        internal static Cell Create(Hex hex, int radius)
        {
            NeighborSet neighbors = HexMath.CreateNeighborSet(hex, radius);
            return new Cell(false, neighbors);
        }

        public readonly bool TryGetCorner(out GridCorner corner) => Neighbors.TryGetCorner(out corner);

        public readonly bool TryGetEdge(out GridEdge edge) => Neighbors.TryGetEdge(out edge);

        public T Content;


        public override readonly string ToString() => $"{{Cell {(IsOccupied ? $"{Content}" : "Empty")}, Neighbors {Neighbors} }}";


    }

    /// <summary>The piece at <paramref name="hex"/>, or null if the cell is empty.</summary>
    public T? this[Hex hex] => TryGet(hex, out var piece) ? piece : null;

    public bool IsOccupied(Hex hex) => _cells[IndexOf(hex)].IsOccupied;

    public void Place(Hex hex, T piece)
    {
        ref var cell = ref _cells[IndexOf(hex)];
        cell.IsOccupied = true;
        cell.Content = piece;
    }

    /// <summary>Empties the cell at <paramref name="hex"/>. Returns false if it was already empty.</summary>
    public bool Remove(Hex hex)
    {
        ref var cell = ref _cells[hex.Index];
        var wasOccupied = cell.IsOccupied;
        cell.IsOccupied = false;
        cell.Content = default;
        return wasOccupied;
    }

    /// <summary>Moves the piece at <paramref name="from"/> to <paramref name="to"/>, replacing whatever is there.</summary>
    public void Move(Hex from, Hex to)
    {
        if (!TryGet(from, out var piece))
        {
            throw new InvalidOperationException($"There is no piece at {from}.");
        }
        Remove(from);
        Place(to, piece);
    }

    /// <summary>Returns true and the piece if <paramref name="hex"/> is in the grid and occupied.</summary>
    public bool TryGet(Hex hex, out T piece)
    {
        if (Contains(hex) && _cells[hex.Index] is { IsOccupied: true } cell)
        {
            piece = cell.Content;
            return true;
        }
        piece = default;
        return false;
    }

    private int IndexOf(Hex hex) => Contains(hex)
            ? hex.Index
            : throw new ArgumentOutOfRangeException(nameof(hex), hex, $"Not within a grid of radius {Radius}.");


    public bool Contains(Hex hex) => HexMath.IsOnBoard(hex, Radius);

    /// <summary>The hexes next to <paramref name="hex"/> that are on the board, as an allocation-free enumeration.</summary>
    public NeighborEnumerator Neighbors(Hex hex) => new NeighborEnumerator(Radius, hex);

    public ref struct NeighborEnumerator //  structurally a : IEnumerator<Hex>, IEnumerable<Hex> but won't ever be used boxed
    {
        private readonly int _radius;
        private readonly Hex _hex;
        private NeighborSet _remainingNeighbors;

        internal NeighborEnumerator(int radius, Hex hex)
        {
            _radius = radius;
            _hex = hex;
            _remainingNeighbors = NeighborSet.AllDirections;
            Current = default;
        }

        public Hex Current { get; private set; }

        public readonly NeighborEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (_remainingNeighbors.TryGetNext(out var neighbor))
            {
                _remainingNeighbors = _remainingNeighbors.Remove(neighbor);

                var neighborHex = _hex + neighbor.Direction;
                if (HexMath.IsOnBoard(neighborHex, _radius))
                {
                    Current = neighborHex;
                    return true;
                }
            }

            return false;
        }
    }
}
