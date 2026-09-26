namespace Gamewright.SquareBoard;

/// <summary>
/// A rectangular board of <see cref="Files"/> × <see cref="Ranks"/> squares.
/// </summary>
/// <remarks>
/// The grid tracks occupancy itself, so <typeparamref name="T"/> only describes real pieces
/// and needs no "empty" value.
/// </remarks>
public class SquareGrid<T> where T : struct
{
    private readonly T?[] _cells;

    public SquareGrid(int files, int ranks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(files);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ranks);

        Files = files;
        Ranks = ranks;
        _cells = new T?[files * ranks];
    }

    public int Files { get; }

    public int Ranks { get; }

    public int Count => _cells.Length;

    /// <summary>All squares of the grid, rank by rank.</summary>
    public IEnumerable<SquareCoord> Coords
    {
        get
        {
            for (var rank = 0; rank < Ranks; rank++)
            {
                for (var file = 0; file < Files; file++)
                {
                    yield return new SquareCoord(file, rank);
                }
            }
        }
    }

    public bool Contains(SquareCoord square)
        => square.File >= 0 && square.File < Files && square.Rank >= 0 && square.Rank < Ranks;

    /// <summary>The piece at <paramref name="square"/>, or null if it is empty.</summary>
    public T? this[SquareCoord square] => _cells[IndexOf(square)];

    public bool IsOccupied(SquareCoord square) => _cells[IndexOf(square)].HasValue;

    /// <summary>Returns true and the piece if <paramref name="square"/> is on the grid and occupied.</summary>
    public bool TryGet(SquareCoord square, out T piece)
    {
        if (Contains(square) && _cells[IndexOf(square)] is { } value)
        {
            piece = value;
            return true;
        }
        piece = default;
        return false;
    }

    public void Place(SquareCoord square, T piece) => _cells[IndexOf(square)] = piece;

    /// <summary>Empties <paramref name="square"/>. Returns false if it was already empty.</summary>
    public bool Remove(SquareCoord square)
    {
        var index = IndexOf(square);
        var wasOccupied = _cells[index].HasValue;
        _cells[index] = null;
        return wasOccupied;
    }

    /// <summary>Moves the piece at <paramref name="from"/> to <paramref name="to"/>, replacing whatever is there.</summary>
    public void Move(SquareCoord from, SquareCoord to)
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
    public IEnumerable<SquareCoord> Ray(SquareCoord from, SquareDirection direction)
    {
        for (var square = from.Neighbor(direction); Contains(square); square = square.Neighbor(direction))
        {
            yield return square;
        }
    }

    private int IndexOf(SquareCoord square) => Contains(square)
        ? square.Rank * Files + square.File
        : throw new ArgumentOutOfRangeException(nameof(square), square, $"Not on a {Files}×{Ranks} grid.");
}
