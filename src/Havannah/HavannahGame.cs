namespace Havannah;

/// <summary>
/// Represents the state and rules of a Havannah game.
/// Manages the board state, active player, and piece placement logic.
/// Pieces are stored in a one-dimensional array indexed by the hexagon's index.
/// </summary>
public class HavannahGame(int radius = 8)
{
    /// <summary>
    /// Number of cells from center to grid's outer edge.
    /// Also the length of each edge of the board.
    /// </summary>
    public int Radius { get; } = radius;

    public HexGrid<HavannahPiece> Grid { get; } = new HexGrid<HavannahPiece>(radius);

    private HavannahPiece _activePlayer = HavannahPiece.Red;

    public HavannahPiece ActivePlayer => _activePlayer;

    /// <summary>The wins created by the most recent move; empty if it did not win.</summary>
    public IReadOnlyList<Win<HavannahPiece>> Wins { get; private set; } = [];

    public HavannahPiece? this[Hex hex]
    {
        get => Grid[hex];
    }

    public void Play(Hex hex)
    {
        if (this[hex] != null)
        {
            throw new InvalidOperationException("Cell is already occupied.");
        }
        Grid.Place(hex, _activePlayer);
        Wins = HavannahStructures.FindWins(Grid, hex);
        _activePlayer = _activePlayer.Opponent;

    }
}
