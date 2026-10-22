namespace Havannah.Tests;

public class HavannahGameTests
{
    private static readonly Hex Center = (0, 0, 0);
    private static readonly Hex[] AroundCenter =
        [(1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1)];

    [Fact]
    public void ANewGameStartsWithRedAndAnEmptyBoard()
    {
        var game = new HavannahGame(radius: 5);

        Assert.Equal(5, game.Radius);
        Assert.Equal(HavannahPiece.Red, game.ActivePlayer);
        Assert.Empty(game.Wins);
        Assert.All(HexMath.EnumerateHexes(5), pair => Assert.Null(game[pair.Item1]));
    }

    [Fact]
    public void DefaultRadiusIsEight() => Assert.Equal(8, new HavannahGame().Radius);

    [Fact]
    public void PlayersAlternateAndPiecesStayWhereTheyWerePlayed()
    {
        var game = new HavannahGame(5);

        game.Play(Center);
        game.Play(AroundCenter[0]);

        Assert.Equal(HavannahPiece.Red, game[Center]);
        Assert.Equal(HavannahPiece.Blue, game[AroundCenter[0]]);
        Assert.Equal(HavannahPiece.Red, game.ActivePlayer);
    }

    [Fact]
    public void AnOccupiedCellCannotBePlayedAgain()
    {
        var game = new HavannahGame(5);
        game.Play(Center);

        Assert.Throws<InvalidOperationException>(() => game.Play(Center));
        Assert.Equal(HavannahPiece.Blue, game.ActivePlayer);
    }

    [Fact]
    public void ACellOutsideTheBoardCannotBePlayed()
    {
        var game = new HavannahGame(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Play((3, -3, 0)));
    }

    [Fact]
    public void TheWinningMoveReportsItsWinAndTheNextMoveClearsIt()
    {
        var game = new HavannahGame(5);
        // Red completes a ring around the center; Blue plays far away cells in between.
        Hex[] blue = [(4, -4, 0), (-4, 4, 0), (0, 4, -4), (0, -4, 4), (4, 0, -4)];
        for (var i = 0; i < 5; i++)
        {
            game.Play(AroundCenter[i]);
            game.Play(blue[i]);
            Assert.Empty(game.Wins);
        }

        game.Play(AroundCenter[5]);

        var win = Assert.Single(game.Wins);
        Assert.Equal(WinKind.Ring, win.Kind);
        Assert.Equal(HavannahPiece.Red, win.Group);

        game.Play((2, -2, 0));
        Assert.Empty(game.Wins);
    }

    [Fact]
    public void OpponentIsTheOtherPlayer()
    {
        Assert.Equal(HavannahPiece.Blue, HavannahPiece.Red.Opponent);
        Assert.Equal(HavannahPiece.Red, HavannahPiece.Blue.Opponent);
        Assert.Throws<ArgumentOutOfRangeException>(() => ((HavannahPiece)9).Opponent);
    }
}
