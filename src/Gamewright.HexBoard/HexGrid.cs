namespace Gamewright.HexBoard;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// a "circular" hexagonal grid representation. 
/// Each hexagon is at most <see cref="Radius"/> hexagons away from the center hexagon. 
/// (0 is the degenerate case with just one hexagon)
/// </summary>
/// <remarks>
/// The grid tracks occupancy itself, so <typeparamref name="T"/> only describes real pieces
/// and needs no "empty" value.
/// </remarks>
public class HexGrid<T> where T : struct
{
    private readonly Cell[] _cells;
    readonly ushort[] _edgeMask;
    readonly ushort[] _cornerMask;

    public int Radius { get; }

    public int Count { get; }

    /// <summary>All coordinates of the grid.</summary>
    public IEnumerable<Coordinate> Coords => CubeMath.CubeHexRegion(Radius);

    public HexGrid(int radius)
    {
        Radius = radius;
        Count = CubeMath.NumberOfHexagonsInGrid(radius);

        _cells = new Cell[Count];
        _edgeMask = new ushort[Count];
        _cornerMask = new ushort[Count];

        // foreach (var hex in CubeMath.CubeHexRegion(radius))
        foreach (var (hex, index) in CubeMath.EnumerateGridCoords(radius))
        {
            if (hex.IsCorner(radius, out var corner))
            {
                var bitIndex = (int)corner;
                _cornerMask[index] |= (ushort)(1 << bitIndex);
            }

            if (hex.IsEdge(radius, out var edge))
            {
                var bitIndex = (int)edge;
                _edgeMask[index] |= (ushort)(1 << bitIndex);
            }

            // initialize the cell at this index. 
            // Might be redundant since the default already sets Occupance to false and Content to default(T)
            _cells[index] = new Cell();
            var cell = _cells[index];

            // neighbors outside the grid are left out
            ref var neighbors = ref _cells[index].Neighbors;
            foreach (var direction in Directions.All)
            {
                var neighbor = hex + direction;
                if (neighbor.IsWithin(radius)) { neighbors.Add((short)neighbor.GetIndex()); }
            }
        }
    }


    /// <summary>
    /// Represents a single cell within the hexagonal grid. Contains occupancy status, content, and neighbor information.
    /// Cell's default state is unoccupied with no content.
    /// </summary>
    public struct Cell
    {
        public bool IsOccupied { get; set; }

        public T Content;

        public NeighborSet Neighbors;

        public override readonly string ToString() => $"{{Cell {(IsOccupied ? $"{Content}" : "Empty")}, Neighbors {Neighbors} }}";
    }

    public string Format(Func<T, char> symbol)
    {
        var totalLength = 0;
        for (var r = Radius; r >= -Radius; r--)
        {
            var (pMin, pMax) = HexRowRange(Radius, r);
            var cellsInRow = pMax - pMin + 1;
            totalLength += Math.Abs(r) + (cellsInRow * 2) + 1;
        }

        return string.Create(totalLength, this, (span, grid) =>
        {
            var cursor = 0;
            // the second component, r, is the row
            for (var r = grid.Radius; r >= -grid.Radius; r--)
            {
                // indent for the current row based on its distance from the center
                for (var i = 0; i < Math.Abs(r); i++)
                {
                    span[cursor++] = ' ';
                }
                // iterate over the columns in the current row
                var (pMin, pMax) = HexRowRange(grid.Radius, r);
                for (var p = pMin; p <= pMax; p++)
                {
                    var hex = new Coordinate(p, -(p + r), r);
                    if (grid.Contains(hex))
                    {
                        span[cursor] = grid.TryGet(hex, out var piece) ? symbol(piece) : '.';
                        var cellIndex = hex.GetIndex();
                        if (grid._edgeMask[cellIndex] != 0)
                        {
                            span[cursor] = int.TrailingZeroCount(grid._edgeMask[cellIndex]).ToString()[0];
                            // span[cursor] = '*';
                        }
                        if (grid._cornerMask[cellIndex] != 0)
                        {
                            span[cursor] = int.TrailingZeroCount(grid._cornerMask[cellIndex]).ToString()[0];
                            // span[cursor] = '*';
                        }

                        cursor++;
                    }
                    else
                    {
                        span[cursor++] = '?';
                    }

                    span[cursor++] = ' ';
                }

                span[cursor++] = '\n';
            }
        });

        static (int Min, int Max) HexRowRange(int radius, int row)
        {
            var min = Math.Max(-radius, -row - radius);
            var max = Math.Min(radius, -row + radius);
            return (min, max);
        }
    }

    /// <summary>The piece at <paramref name="hex"/>, or null if the cell is empty.</summary>
    public T? this[Coordinate hex] => TryGet(hex, out var piece) ? piece : null;

    public bool IsOccupied(Coordinate hex) => _cells[IndexOf(hex)].IsOccupied;

    public void Place(Coordinate hex, T piece)
    {
        ref var cell = ref _cells[IndexOf(hex)];
        cell.IsOccupied = true;
        cell.Content = piece;
    }

    /// <summary>Empties the cell at <paramref name="hex"/>. Returns false if it was already empty.</summary>
    public bool Remove(Coordinate hex)
    {
        ref var cell = ref _cells[IndexOf(hex)];
        var wasOccupied = cell.IsOccupied;
        cell.IsOccupied = false;
        cell.Content = default;
        return wasOccupied;
    }

    /// <summary>Moves the piece at <paramref name="from"/> to <paramref name="to"/>, replacing whatever is there.</summary>
    public void Move(Coordinate from, Coordinate to)
    {
        if (!TryGet(from, out var piece))
        {
            throw new InvalidOperationException($"There is no piece at {from}.");
        }
        Remove(from);
        Place(to, piece);
    }

    /// <summary>Returns true and the piece if <paramref name="hex"/> is in the grid and occupied.</summary>
    public bool TryGet(Coordinate hex, out T piece)
    {
        if (Contains(hex) && _cells[hex.GetIndex()] is { IsOccupied: true } cell)
        {
            piece = cell.Content;
            return true;
        }
        piece = default;
        return false;
    }

    private int IndexOf(Coordinate hex) => Contains(hex)
        ? hex.GetIndex()
        : throw new ArgumentOutOfRangeException(nameof(hex), hex, $"Not within a grid of radius {Radius}.");

    /// <summary>Grid-geometry data for callers composing region/connectivity tracking on top of the grid.</summary>
    public ReadOnlySpan<short> Neighbors(int index) => _cells[index].Neighbors.AsReadOnlySpan();

    public ushort EdgeMask(int index) => _edgeMask[index];

    public ushort CornerMask(int index) => _cornerMask[index];
    // public bool TryGetCellContent(int q, int r, int s, [MaybeNullWhen(false)] out T content)
    // {
    //     try
    //     {
    //         var hex = new Coordinate(q, r, s);
    //         return TryGetCellContent(hex, out content);
    //     }
    //     catch (ArgumentOutOfRangeException)
    //     {
    //         content = default;
    //         return false;
    //     }
    // }

    public bool Contains(Coordinate hex) => hex.IsWithin(Radius);

    public bool IsCorner(Coordinate hex, [MaybeNullWhen(false)] out GridCornerDirection dir)
    {
        var ix = hex.GetIndex();
        dir = default!;
        return _cornerMask[ix] != 0;
    }

    public bool IsEdge(Coordinate hex, [MaybeNullWhen(false)] out GridEdgeDirection dir)
    {
        var ix = hex.GetIndex();
        dir = default!;
        return _edgeMask[ix] != 0;
    }

    public GridLocationKind GetKind(Coordinate hex)
    {
        var ix = hex.GetIndex();
        var (c, e) = (_cornerMask[ix], _edgeMask[ix]);
        var kind = (c, e) switch
        {
            (0, 0) => GridLocationKind.Inner,
            (_, 0) => GridLocationKind.Corner,
            (0, _) => GridLocationKind.Edge,
            _ => throw new InvalidOperationException("Unexpected corner/edge mask combination")
        };
        return kind;
    }
}

