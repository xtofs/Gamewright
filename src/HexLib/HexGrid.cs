namespace HexLib;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;


/// <summary>
/// a "circular" hexagonal grid representation. 
/// Each hexagon is at most <see cref="Radius"/> hexagons away from the center hexagon. 
/// (0 is the degenerate case with just one hexagon)
/// </summary>
partial class HexGrid<T> where T : ICellContent
{
    private readonly Cell[] _board;
    readonly ushort[] _edgeMask;
    readonly ushort[] _cornerMask;

    public int Radius { get; }

    public int Count { get; }

    public HexGrid(int radius)
    {
        Radius = radius;
        Count = CubeMath.NumberOfHexagonsInGrid(radius);

        _board = new Cell[Count];
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
        }
    }


    [InlineArray(6)]
    public struct NeighborArray
    {
        private int _element0;
    }

    public struct Cell
    {
        public T State;
        public NeighborArray Neighbors;

        public override readonly string ToString() => $"Cell(State={State.Symbol})";
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

    public void PlaceStone(CubeCoord cube, T player)
    {
        var index = cube.GetIndex();
        PlaceStone(index, player);
    }

    internal void PlaceStone(int idx, T content)
    {
        ref var cell = ref _board[idx];
        cell.State = content;
    }

    /// <summary>Grid-geometry data for callers composing region/connectivity tracking on top of the grid.</summary>
    public ReadOnlySpan<int> Neighbors(int index) => _board[index].Neighbors;

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

    public bool TryGetContent(CubeCoord cube, [MaybeNullWhen(false)] out T content)
    {
        var (q, r, s) = cube;
        if (Math.Abs(q) > Radius || Math.Abs(r) > Radius || Math.Abs(s) > Radius)
        {
            content = default;
            return false;
        }

        var index = cube.GetIndex();
        if (index >= _board.Length || index < 0)
        {
            content = default;
            return false;
        }

        content = _board[index].State;
        return true;
    }
}


