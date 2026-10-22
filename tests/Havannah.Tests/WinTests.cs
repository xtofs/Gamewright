namespace Havannah.Tests;

public class WinTests
{
    private const int Radius = 5;

    private static readonly Hex Center = (0, 0, 0);
    private static readonly Hex[] AroundCenter =
        [(1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1)];

    private static HexGrid<HavannahPiece> Board(HavannahPiece piece, IEnumerable<Hex> hexes)
    {
        var grid = new HexGrid<HavannahPiece>(Radius);
        foreach (var hex in hexes) { grid.Place(hex, piece); }
        return grid;
    }

    private static HexGrid<HavannahPiece> Red(params IEnumerable<Hex>[] groups) =>
        Board(HavannahPiece.Red, groups.SelectMany(g => g));

    /// <summary>Cells from the center towards an edge, alternating two neighbor directions.</summary>
    private static IEnumerable<Hex> Arm(Hex a, Hex b, int length)
    {
        var (q, r, s) = (0, 0, 0);
        for (var i = 0; i < length; i++)
        {
            var (dq, dr, ds) = i % 2 == 0 ? a : b;
            (q, r, s) = (q + dq, r + dr, s + ds);
            yield return new Hex(q, r, s);
        }
    }

    private static IEnumerable<Hex> Spoke(int clockwiseThirds) =>
        Arm(Rotate((0, -1, 1), clockwiseThirds), Rotate((1, -1, 0), clockwiseThirds), 4);

    /// <summary>Rotates a direction by 120° clockwise, <paramref name="thirds"/> times: (q, r, s) → (s, q, r).</summary>
    private static Hex Rotate(Hex hex, int thirds)
    {
        var (q, r, s) = hex;
        for (var i = 0; i < thirds; i++) { (q, r, s) = (s, q, r); }
        return new Hex(q, r, s);
    }

    private static IEnumerable<WinKind> Kinds(HexGrid<HavannahPiece> grid, Hex hex) =>
        HavannahStructures.FindWins(grid, hex).Select(w => w.Kind);

    [Fact]
    public void RingOfSixAroundCenter()
    {
        var grid = Red(AroundCenter);
        var win = Assert.Single(HavannahStructures.FindWins(grid, AroundCenter[0]));
        Assert.Equal(WinKind.Ring, win.Kind);
        Assert.Equal(6, win.Cells.Count);
    }

    [Fact]
    public void RingWithGapIsNoWin() =>
        Assert.Empty(HavannahStructures.FindWins(Red(AroundCenter.Skip(1)), AroundCenter[1]));

    [Fact]
    public void RingEnclosesOpponentStones()
    {
        var grid = Red(AroundCenter);
        grid.Place(Center, HavannahPiece.Blue);
        Assert.Contains(WinKind.Ring, Kinds(grid, AroundCenter[0]));
    }

    [Fact]
    public void RingOnTheBoardBorder()
    {
        // the 6 neighbors of an edge-adjacent cell on ring 3
        var hole = new Hex(0, -3, 3);
        var ring = new[] { (1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1) }
            .Select(d => new Hex(hole.Q + d.Item1, hole.R + d.Item2, hole.S + d.Item3));
        Assert.Contains(WinKind.Ring, Kinds(Red(ring), ring.First()));
    }

    [Fact]
    public void LineHasNoRing() =>
        Assert.DoesNotContain(WinKind.Ring, Kinds(Red(Arm((1, 0, -1), (1, 0, -1), 4), [Center]), Center));

    [Fact]
    public void MixedColorsDoNotMakeARing()
    {
        var grid = Red(AroundCenter.Take(3));
        foreach (var hex in AroundCenter.Skip(3)) { grid.Place(hex, HavannahPiece.Blue); }
        Assert.Empty(HavannahStructures.FindWins(grid, AroundCenter[0]));
        Assert.Empty(HavannahStructures.FindWins(grid, AroundCenter[4]));
    }

    [Fact]
    public void BridgeConnectsTwoCorners()
    {
        var line = Enumerable.Range(-(Radius - 1), 2 * Radius - 1).Select(t => new Hex(t, -t, 0)).ToList();
        var grid = Red(line);
        var win = Assert.Single(HavannahStructures.FindWins(grid, Center));
        Assert.Equal(WinKind.Bridge, win.Kind);
        Assert.Equal(line.Count, win.Cells.Count);
        Assert.All([win.Cells[0], win.Cells[^1]], h => Assert.True(grid.TryGetCorner(h, out _)));
    }

    [Fact]
    public void SingleCornerIsNoBridge() =>
        Assert.Empty(HavannahStructures.FindWins(Red([new Hex(Radius - 1, -(Radius - 1), 0)]), (Radius - 1, -(Radius - 1), 0)));

    [Fact]
    public void ForkConnectsThreeEdges()
    {
        var grid = Red([Center], Spoke(0), Spoke(1), Spoke(2));
        var win = Assert.Single(HavannahStructures.FindWins(grid, Center));
        Assert.Equal(WinKind.Fork, win.Kind);
        Assert.Equal(13, win.Cells.Count);
        Assert.Equal(3, win.Cells.Count(h => grid.TryGetEdge(h, out _)));
    }

    [Fact]
    public void TwoEdgesAreNoFork() =>
        Assert.Empty(HavannahStructures.FindWins(Red([Center], Spoke(0), Spoke(1)), Center));

    [Fact]
    public void EmptyCellHasNoWins() =>
        Assert.Empty(HavannahStructures.FindWins(Red(AroundCenter), Center));

    [Fact]
    public void RegionCanHaveSeveralWins()
    {
        // a corner-to-corner line plus a ring around a cell next to it
        var line = Enumerable.Range(-(Radius - 1), 2 * Radius - 1).Select(t => new Hex(t, -t, 0));
        var hole = new Hex(0, 1, -1);
        var ring = AroundCenter.Select(d => new Hex(hole.Q + d.Q, hole.R + d.R, hole.S + d.S));
        var kinds = Kinds(Red(line, ring), Center).ToList();
        Assert.Contains(WinKind.Bridge, kinds);
        Assert.Contains(WinKind.Ring, kinds);
    }
}
