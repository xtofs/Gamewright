namespace Gamewright.SquareBoard;

/// <summary>
/// The eight directions from a square to its neighbors, clockwise from North.
/// North is increasing rank, East is increasing file.
/// </summary>
public enum SquareDirection
{
    North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
}

public static class SquareDirections
{
    /// <summary>The four directions a rook moves in.</summary>
    public static IReadOnlyList<SquareDirection> Orthogonal { get; } =
        [SquareDirection.North, SquareDirection.East, SquareDirection.South, SquareDirection.West];

    /// <summary>The four directions a bishop moves in.</summary>
    public static IReadOnlyList<SquareDirection> Diagonal { get; } =
        [SquareDirection.NorthEast, SquareDirection.SouthEast, SquareDirection.SouthWest, SquareDirection.NorthWest];

    /// <summary>All eight directions, the ones a queen moves in.</summary>
    public static IReadOnlyList<SquareDirection> All { get; } = Enum.GetValues<SquareDirection>();

    extension(SquareDirection direction)
    {
        public SquareDirection Opposite() => (SquareDirection)(((int)direction + 4) % 8);
    }
}
