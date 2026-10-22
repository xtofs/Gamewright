namespace Gamewright.HexagonalGrid.Tests;

public class NeighborSetTests
{
    [Fact]
    public void AddRemoveAndHas()
    {
        var set = default(NeighborSet).Add(Neighbor.ThreeOClock).Add(Neighbor.NineOClock);

        Assert.True(set.Has(Neighbor.ThreeOClock));
        Assert.True(set.Has(Neighbor.NineOClock));
        Assert.False(set.Has(Neighbor.OneOClock));

        set = set.Remove(Neighbor.ThreeOClock);
        Assert.False(set.Has(Neighbor.ThreeOClock));
        Assert.True(set.Has(Neighbor.NineOClock));
    }

    [Fact]
    public void InverseIsTheComplementWithinTheSixDirections()
    {
        var set = NeighborSet.OneOClock | NeighborSet.FiveOClock;

        Assert.Equal(NeighborSet.ThreeOClock | NeighborSet.SevenOClock | NeighborSet.NineOClock | NeighborSet.ElevenOClock, set.Inverse);
        Assert.Equal(default, NeighborSet.AllDirections.Inverse);
    }

    [Fact]
    public void TryGetNextWalksTheMembersInOrderAndStopsWhenEmpty()
    {
        var set = NeighborSet.FiveOClock | NeighborSet.OneOClock | NeighborSet.NineOClock;
        var seen = new List<Neighbor>();

        while (set.TryGetNext(out var next))
        {
            seen.Add(next);
            set = set.Remove(next);
        }

        Assert.Equal([Neighbor.OneOClock, Neighbor.FiveOClock, Neighbor.NineOClock], seen);
    }

    [Fact]
    public void DirectionsAreSixDistinctSteps()
    {
        var directions = Neighbor.All.Select(n => n.Direction).ToList();

        Assert.Equal(6, directions.Distinct().Count());
        Assert.All(directions, d => Assert.Equal(1, new Hex(0, 0, 0).Ring + (new Hex(0, 0, 0) + d).Ring));
    }

    [Fact]
    public void OppositeDirectionsCancel()
    {
        for (var i = 0; i < 3; i++)
        {
            var a = Neighbor.All[i].Direction;
            var b = Neighbor.All[i + 3].Direction;
            Assert.Equal((0, 0, 0), (a.Q + b.Q, a.R + b.R, a.S + b.S));
        }
    }

    [Fact]
    public void ThreeMissingNeighborsInARowMakeACorner()
    {
        var set = NeighborSet.AllDirections
            .Remove(Neighbor.NineOClock).Remove(Neighbor.ElevenOClock).Remove(Neighbor.OneOClock);

        Assert.True(set.TryGetCorner(out var corner));
        Assert.Equal(GridCorner.ElevenOClock, corner);
        Assert.False(set.TryGetEdge(out _));
    }

    [Fact]
    public void TwoMissingNeighborsInARowMakeAnEdge()
    {
        var set = NeighborSet.AllDirections.Remove(Neighbor.OneOClock).Remove(Neighbor.ThreeOClock);

        Assert.True(set.TryGetEdge(out var edge));
        Assert.Equal(GridEdge.TwoOClock, edge);
        Assert.False(set.TryGetCorner(out _));
    }

    [Fact]
    public void AFullSetIsNeitherCornerNorEdge()
    {
        Assert.False(NeighborSet.AllDirections.TryGetCorner(out _));
        Assert.False(NeighborSet.AllDirections.TryGetEdge(out _));
    }
}
