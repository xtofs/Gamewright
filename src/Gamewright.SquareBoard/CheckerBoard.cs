namespace Gamewright.SquareBoard;

/// <summary>
/// A rectangular board of <see cref="Columns"/> × <see cref="Rows"/> squares.
/// </summary>
/// <remarks>
/// The grid tracks occupancy itself, so <typeparamref name="T"/> only describes real pieces
/// and needs no "empty" value.
/// </remarks>
public class CheckerBoard<T> where T : struct
{
    private readonly T?[] _cells;

    public CheckerBoard(int files, int ranks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(files);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ranks);

        Columns = files;
        Rows = ranks;
        _cells = new T?[files * ranks];
    }

    public int Columns { get; }

    public int Rows { get; }

    public int Count => _cells.Length;

    /// <summary>All squares of the grid, rank by rank.</summary>
    public IEnumerable<Coordinate> Coords
    {
        get
        {
            for (var rank = 0; rank < Rows; rank++)
            {
                for (var file = 0; file < Columns; file++)
                {
                    yield return new Coordinate(file, rank);
                }
            }
        }
    }

    public bool IsValid(Coordinate square) =>
       square.Column >= 0 && square.Column < Columns && square.Row >= 0 && square.Row < Rows;

    /// <summary>The piece at <paramref name="square"/>, or null if it is empty.</summary>
    public T? this[Coordinate square] => _cells[IndexOf(square)];

    public bool IsOccupied(Coordinate square) => _cells[IndexOf(square)].HasValue;

    /// <summary>Returns true and the piece if <paramref name="square"/> is on the grid and occupied.</summary>
    public bool TryGet(Coordinate square, out T piece)
    {
        if (IsValid(square) && _cells[IndexOf(square)] is { } value)
        {
            piece = value;
            return true;
        }
        piece = default;
        return false;
    }

    public void Place(Coordinate square, T piece) => _cells[IndexOf(square)] = piece;

    /// <summary>Empties <paramref name="square"/>. Returns false if it was already empty.</summary>
    public bool Remove(Coordinate square)
    {
        var index = IndexOf(square);
        var wasOccupied = _cells[index].HasValue;
        _cells[index] = null;
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

    /// <summary>
    /// The squares from <paramref name="from"/> (exclusive) in <paramref name="direction"/>
    /// up to the edge of the grid, regardless of occupancy.
    /// </summary>
    public IEnumerable<Coordinate> Ray(Coordinate from, Direction direction)
    {
        for (var square = from + direction; IsValid(square); square = square + direction)
        {
            yield return square;
        }
    }

    private int IndexOf(Coordinate square) => IsValid(square)
        ? square.Row * Columns + square.Column
        : throw new ArgumentOutOfRangeException(nameof(square), square, $"Not on a {Columns}×{Rows} grid.");

}
