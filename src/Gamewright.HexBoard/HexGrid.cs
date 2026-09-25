namespace Gamewright.HexBoard;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// a "circular" hexagonal grid representation. 
/// Each hexagon is at most <see cref="Radius"/> hexagons away from the center hexagon. 
/// (0 is the degenerate case with just one hexagon)
/// </summary>
public partial class HexGrid<T> where T : ICellContent
{
    private readonly Cell[] _cells;
    readonly ushort[] _edgeMask;
    readonly ushort[] _cornerMask;

    public int Radius { get; }

    public int Count { get; }

    public HexGrid(int radius)
    {
        Radius = radius;
        Count = CubeMath.NumberOfHexagonsInGrid(radius);

        _cells = new Cell[Count];
        _edgeMask = new ushort[Count];
        _cornerMask = new ushort[Count];

        // foreach (var cube in CubeMath.CubeHexRegion(radius))
        foreach (var (cube, index) in CubeMath.EnumerateGridCoords(radius))
        {
            if (cube.IsCorner(radius, out var corner))
            {
                var bitIndex = (int)corner;
                _cornerMask[index] |= (ushort)(1 << bitIndex);
            }

            if (cube.IsEdge(radius, out var edge))
            {
                var bitIndex = (int)edge;
                _edgeMask[index] |= (ushort)(1 << bitIndex);
            }

            // initialize the cell at this index. 
            // Might be redundant since the default already sets Occupance to false and Content to default(T)
            _cells[index] = new Cell();
            var cell = _cells[index];

            // neighbor slots are indexed by HexDirection, -1 marks a neighbor outside the grid
            ref var neighbors = ref _cells[index].Neighbors;
            foreach (var direction in Enum.GetValues<HexDirection>())
            {
                var neighbor = cube.Neighbor(direction);
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

    public string Format()
    {
        var totalLength = 0;
        for (var r = Radius; r >= -Radius; r--)
        {
            var (pMin, pMax) = HexRowRange(Radius, r);
            var cellsInRow = pMax - pMin + 1;
            totalLength += Math.Abs(r) + (cellsInRow * 2) + 1;
        }

        return string.Create(totalLength, this, static (span, grid) =>
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
                    var cube = new CubeCoord(p, -(p + r), r);
                    if (grid.TryGetContent(cube, out var content))
                    {
                        span[cursor] = content.Symbol == '\0' ? '.' : content.Symbol;
                        var cellIndex = cube.GetIndex();
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

    public void PlacePiece(CubeCoord cube, T piece)
    {
        var index = cube.GetIndex();
        PlaceStone(index, piece);
    }

    internal void PlaceStone(int idx, T content)
    {
        ref var cell = ref _cells[idx];
        cell.IsOccupied = true;
        cell.Content = content;
    }

    /// <summary>Grid-geometry data for callers composing region/connectivity tracking on top of the grid.</summary>
    public ReadOnlySpan<short> Neighbors(int index) => _cells[index].Neighbors;

    public ushort EdgeMask(int index) => _edgeMask[index];

    public ushort CornerMask(int index) => _cornerMask[index];
    // public bool TryGetCellContent(int q, int r, int s, [MaybeNullWhen(false)] out T content)
    // {
    //     try
    //     {
    //         var cube = new CubeCoord(q, r, s);
    //         return TryGetCellContent(cube, out content);
    //     }
    //     catch (ArgumentOutOfRangeException)
    //     {
    //         content = default;
    //         return false;
    //     }
    // }

    public bool Contains(CubeCoord cube) => cube.IsWithin(Radius);

    public bool TryGetContent(CubeCoord cube, [MaybeNullWhen(false)] out T content)
    {
        var index = cube.GetIndex();
        if (index >= _cells.Length || index < 0)
        {
            content = default;
            return false;
        }
        if (_cells[index].IsOccupied)
        {
            content = _cells[index].Content;
            return true;
        }
        content = default;
        return false;
    }

    public bool IsCorner(CubeCoord hex, [MaybeNullWhen(false)] out GridCornerDirection dir)
    {
        var ix = hex.GetIndex();
        dir = default!;
        return _cornerMask[ix] != 0;
    }

    public bool IsEdge(CubeCoord hex, [MaybeNullWhen(false)] out GridEdgeDirection dir)
    {
        var ix = hex.GetIndex();
        dir = default!;
        return _edgeMask[ix] != 0;
    }

    public GridLocationKind GetKind(CubeCoord hex)
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

