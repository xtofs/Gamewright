namespace Gamewright.Graphics;

using System.Collections;

public record struct ColumnDefinition(GridLength Width);

public record struct RowDefinition(GridLength Height);

public record struct GridLength(float Value, GridLengthUnit Unit);

public enum GridLengthUnit
{
    Proportional,
    Absolute,
    Auto, // not yet implemented — requires two-pass measure/arrange
}

public sealed class Grid : Node
{
    // Covered positions (from spans) store the owning cell reference rather than null,
    // so TwoDArrayView<Cell> can be used without nullable elements.
    private readonly Cell[,] _cells;
    private readonly TwoDArrayView<Cell> _view;

    public ColumnDefinition[] ColumnDefinitions { get; }
    public RowDefinition[] RowDefinitions { get; }

    public Grid(ColumnDefinition[] columnDefinitions, RowDefinition[] rowDefinitions)
    {
        ColumnDefinitions = columnDefinitions;
        RowDefinitions = rowDefinitions;

        var cols = columnDefinitions.Length;
        var rows = rowDefinitions.Length;
        _cells = new Cell[cols, rows];

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                _cells[c, r] = new Cell(c, r);
            }
        }

        _view = new TwoDArrayView<Cell>(_cells);
    }

    // IReadOnlyList<Cell> is assignable to IReadOnlyList<Node> via covariance.
    public override IReadOnlyList<Node> Children => _view;

    // Grid's structure is fixed at construction; reject ad-hoc children.
    protected override void OnAddChild(Node child)
        => throw new InvalidOperationException("Children of Grid are defined by its column/row definitions.");

    /// <summary>Returns the cell that owns position (col, row).</summary>
    public Cell this[int col, int row] => _cells[col, row];

    /// <summary>
    /// Sets ColSpan and RowSpan for the cell at (col, row).
    /// Covered positions are redirected to the owning cell.
    /// </summary>
    public void SetSpan(int col, int row, int colSpan, int rowSpan)
    {
        if (col < 0 || col + colSpan > ColumnDefinitions.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(colSpan));
        }

        if (row < 0 || row + rowSpan > RowDefinitions.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpan));
        }

        var owner = _cells[col, row];
        if (owner.Col != col || owner.Row != row)
        {
            throw new InvalidOperationException($"Position ({col},{row}) is already covered by another span.");
        }

        owner.ColSpan = colSpan;
        owner.RowSpan = rowSpan;

        for (var r = row; r < row + rowSpan; r++)
        {
            for (var c = col; c < col + colSpan; c++)
            {
                _cells[c, r] = owner; // covered positions point to owner
            }
        }
    }

    protected override Rect Layout(Rect parent) => parent;

    protected override void LayoutChildren()
    {
        var xs = ComputeOffsets(ColumnDefinitions.Length, i => ColumnDefinitions[i].Width, Rect.Width, Rect.X);
        var ys = ComputeOffsets(RowDefinitions.Length, i => RowDefinitions[i].Height, Rect.Height, Rect.Y);

        // _view may contain the same cell multiple times at covered span positions; use a set to process each once.
        var seen = new HashSet<Cell>();
        foreach (var cell in _view)
        {
            if (!seen.Add(cell))
            {
                continue;
            }

            var x = xs[cell.Col];
            var y = ys[cell.Row];
            var w = xs[cell.Col + cell.ColSpan] - x;
            var h = ys[cell.Row + cell.RowSpan] - y;
            cell.Recompute(new Rect(x, y, w, h));
        }
    }

    // Returns an array of length (count+1): offsets[i] = pixel start of track i;
    // offsets[count] = origin + totalSize (the end boundary).
    private static float[] ComputeOffsets(int count, Func<int, GridLength> getLength, float totalSize, float origin)
    {
        var lengths = new float[count];
        float stars = 0f, absolute = 0f;

        for (var i = 0; i < count; i++)
        {
            var gl = getLength(i);
            switch (gl.Unit)
            {
                case GridLengthUnit.Absolute:
                    lengths[i] = gl.Value;
                    absolute += gl.Value;
                    break;
                case GridLengthUnit.Proportional:
                    lengths[i] = gl.Value; // placeholder; scaled below
                    stars += gl.Value;
                    break;
                case GridLengthUnit.Auto:
                    throw new NotSupportedException("GridLengthUnit.Auto is not yet implemented.");
            }
        }

        var perStar = stars > 0f ? (totalSize - absolute) / stars : 0f;
        for (var i = 0; i < count; i++)
        {
            if (getLength(i).Unit == GridLengthUnit.Proportional)
            {
                lengths[i] *= perStar;
            }
        }

        var offsets = new float[count + 1];
        offsets[0] = origin;
        for (var i = 0; i < count; i++)
        {
            offsets[i + 1] = offsets[i] + lengths[i];
        }

        return offsets;
    }

    public sealed class Cell : LeafNode
    {
        public int Col { get; }
        public int Row { get; }
        public int ColSpan { get; internal set; } = 1;
        public int RowSpan { get; internal set; } = 1;

        internal Cell(int col, int row)
        {
            Col = col;
            Row = row;
        }

        protected override Rect Layout(Rect parent) => parent;
    }
}

public sealed class TwoDArrayView<T> : IReadOnlyList<T>
{
    private readonly T[,] _cells;
    private readonly int _dim0; // size of first array dimension
    private readonly int _dim1; // size of second array dimension

    public TwoDArrayView(T[,] cells)
    {
        _cells = cells;
        _dim0 = cells.GetLength(0);
        _dim1 = cells.GetLength(1);
        Count = cells.Length;
    }

    public int Count { get; }

    public T this[int index]
    {
        get
        {
            var i0 = index / _dim1;
            var i1 = index % _dim1;
            return _cells[i0, i1];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i0 = 0; i0 < _dim0; i0++)
        {
            for (var i1 = 0; i1 < _dim1; i1++)
            {
                yield return _cells[i0, i1];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
