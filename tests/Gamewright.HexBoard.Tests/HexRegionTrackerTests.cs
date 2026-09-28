namespace Gamewright.HexBoard.Tests;

public class HexRegionTrackerTests
{
    private const int Radius = 3;

    [Fact]
    public void FindBridgePath_FollowsTheBoardSideBetweenTwoCorners()
    {
        var grid = new HexGrid<int>(Radius);
        var side = BoardSide(Direction.NorthWest, Direction.NorthEast);
        foreach (var cell in side)
        {
            grid.Place(cell, 1);
        }

        var path = new HexRegionTracker<int>(grid).FindBridgePath(side[1].GetIndex());

        Assert.Equal(side.Select(cell => cell.GetIndex()), path);
    }

    [Fact]
    public void FindBridgePath_SkipsCellsOffTheShortestPath()
    {
        var grid = new HexGrid<int>(Radius);
        var side = BoardSide(Direction.NorthWest, Direction.NorthEast);
        foreach (var cell in side)
        {
            grid.Place(cell, 1);
        }

        // a dead end hanging off the middle of the bridge towards the board center
        var blob = new[] { side[1] + Direction.SouthEast, side[1] + 2 * Direction.SouthEast };
        foreach (var cell in blob)
        {
            grid.Place(cell, 1);
        }

        var path = new HexRegionTracker<int>(grid).FindBridgePath(blob[^1].GetIndex());

        Assert.Equal(side.Select(cell => cell.GetIndex()), path);
    }

    [Fact]
    public void FindForkPaths_BranchesFromTheJunctionToThreeDistinctEdges()
    {
        var grid = new HexGrid<int>(Radius);
        var center = Coordinate.Center;
        grid.Place(center, 1);

        // three spokes from the center, each bending to end on a board edge (not a corner)
        foreach (var (outward, along) in new[]
        {
            (Direction.NorthWest, Direction.NorthEast),
            (Direction.East, Direction.SouthEast),
            (Direction.SouthWest, Direction.West),
        })
        {
            grid.Place(center + outward, 1);
            grid.Place(center + 2 * outward, 1);
            grid.Place(center + 2 * outward + along, 1);
        }

        var tracker = new HexRegionTracker<int>(grid);
        var branches = tracker.FindForkPaths(center.GetIndex());

        Assert.Equal(3, branches.Count);
        Assert.All(branches, branch => Assert.Equal(center.GetIndex(), branch[0]));
        Assert.All(branches, branch => Assert.Equal(4, branch.Count));
        Assert.All(branches, AssertConnected);

        var edges = branches.Select(branch => grid.EdgeMask(branch[^1])).ToArray();
        Assert.All(edges, edge => Assert.NotEqual(0, edge));
        Assert.Equal(3, edges.Distinct().Count());
    }

    /// <summary>The cells of the board side from the corner in direction <paramref name="from"/> to the adjacent corner <paramref name="to"/>.</summary>
    private static Coordinate[] BoardSide(Direction from, Direction to)
    {
        var start = Coordinate.Center + Radius * from;
        var step = to.ToOffset() - from.ToOffset();
        return [.. Enumerable.Range(0, Radius + 1).Select(k => start + k * step)];
    }

    private static void AssertConnected(IReadOnlyList<int> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
            var delta = Coordinate.FromIndex(path[i]) - Coordinate.FromIndex(path[i - 1]);
            Assert.Equal(1, delta.Length);
        }
    }
}
