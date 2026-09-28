namespace Amazons;

using System.Diagnostics.CodeAnalysis;
using Gamewright.SquareBoard;

/// <summary>
/// Represents the current state of a player's turn in the game.
/// </summary>
/// <remarks>
/// Represents the current player and their incomplete move in the game.
/// Tracks the selected amazon, its target square, and the arrow's target square.
/// The internal states are Waiting -> AmazonSelected -> AmazonMoved -> Waiting.
/// With an option to cancel AmazonSelected back to Waiting
/// </remarks>
public class SelectionState
{
    public SelectionState()
    {
        Phase = Phase.AwaitingSelection;
        SelectedAmazon = null;
        AmazonTarget = null;
    }

    public Phase Phase { get; private set; }

    public Coordinate? SelectedAmazon { get; private set; }

    public Coordinate? AmazonTarget { get; private set; }

    public bool SelectAmazon(Coordinate amazon)
    {
        if (Phase != Phase.AwaitingSelection) { return false; }
        SelectedAmazon = amazon;
        Phase = Phase.AwaitingMove;
        return true;
    }

    public bool DeselectAmazon()
    {
        if (Phase != Phase.AwaitingMove) { return false; }
        SelectedAmazon = null;
        Phase = Phase.AwaitingSelection;
        return true;
    }

    public bool MoveAmazon(Game game, Coordinate target)
    {
        if (Phase != Phase.AwaitingMove) { return false; }
        if (game.Board.TryGet(target, out var _)) { return false; }
        AmazonTarget = target;
        Phase = Phase.AwaitingShot;
        return true;
    }

    public bool ShootArrow(Game game, Coordinate target, [MaybeNullWhen(false)] out Move move)
    {
        move = default;
        if (Phase != Phase.AwaitingShot) { return false; }
        if (game.Board.TryGet(target, out var _)) { return false; }

        move = new Move(game.CurrentPlayer, SelectedAmazon!.Value, AmazonTarget!.Value, target);
        game.CurrentPlayer = game.CurrentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
        Phase = Phase.AwaitingSelection;
        SelectedAmazon = null;
        AmazonTarget = null;
        return true;
    }
}

public enum Phase { AwaitingSelection, AwaitingMove, AwaitingShot };

public enum Player { Player1, Player2 }

public record Move(Player Player, Coordinate From, Coordinate To, Coordinate Arrow);

public class Game
{
    public const int N = 10;

    public CheckerBoard<Piece> Board { get; }

    public Player CurrentPlayer { get; internal set; }

    public Game()
    {
        CurrentPlayer = Player.Player1;
        var board = new CheckerBoard<Piece>(N, N);
        // https://en.wikipedia.org/wiki/Game_of_the_Amazons
        // Place the initial white queens
        board.Place(new Coordinate(0, 3), Piece.WhiteQueen);
        board.Place(new Coordinate(0, 6), Piece.WhiteQueen);
        board.Place(new Coordinate(3, 0), Piece.WhiteQueen);
        board.Place(new Coordinate(3, 9), Piece.WhiteQueen);

        // Place the initial black queens
        board.Place(new Coordinate(6, 0), Piece.BlackQueen);
        board.Place(new Coordinate(6, 9), Piece.BlackQueen);
        board.Place(new Coordinate(9, 3), Piece.BlackQueen);
        board.Place(new Coordinate(9, 6), Piece.BlackQueen);

        Board = board;
    }

    internal IEnumerable<Coordinate> GetAvailableArrowTargets(Coordinate target)
    {
        foreach (var dir in Directions.OrthogonalAndDiagonal)
        {
            var current = target;
            while (true)
            {
                current += dir;
                // Stop if the square is off the board or occupied
                if (!Board.IsValid(current) || Board.TryGet(current, out var _))
                {
                    break;
                }
                yield return current;
            }
        }
    }
}
