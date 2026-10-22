namespace Havannah;

public static class HavannahStructures
{
    public static Region<T> FindRegion<T>(HexGrid<T> grid, Hex hex) where T : struct
    {
        if (!grid.TryGet(hex, out var group))
        {
            throw new InvalidOperationException($"Cell {hex} is not occupied.");
        }
        var hexIndex = hex.Index;
        var visited = new bool[grid.Count];
        var pending = new Stack<Hex>();
        var members = new List<Hex>();
        HashSet<GridEdge> edges = [];
        HashSet<GridCorner> corners = [];

        visited[hexIndex] = true;
        pending.Push(hex);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            members.Add(current);

            if (grid.TryGetCorner(current, out var corner))
            {
                corners.Add(corner);
            }
            if (grid.TryGetEdge(current, out var edge))
            {
                edges.Add(edge);
            }
            var cmp = EqualityComparer<T>.Default;
            foreach (var neighbor in grid.Neighbors(current))
            {
                int index = neighbor.Index;
                if (visited[index])
                {
                    continue;
                }
                visited[index] = true;

                if (grid.TryGet(neighbor, out var piece) && cmp.Equals(piece, group))
                {
                    pending.Push(neighbor);
                }
            }
        }

        return new Region<T>(group, edges, corners, members);
    }

    /// <summary>Every win condition satisfied by <paramref name="region"/>, with the hexes that demonstrate it.</summary>
    public static IEnumerable<Win<T>> FindWins<T>(HexGrid<T> grid, Region<T> region) where T : struct
    {
        if (TryBridge(grid, region) is { } bridge) { yield return bridge; }
        if (TryFork(grid, region) is { } fork) { yield return fork; }
        if (TryRing(grid, region) is { } ring) { yield return ring; }
    }

    /// <summary>The wins of the region containing <paramref name="hex"/>; empty if the cell is empty.</summary>
    public static IReadOnlyList<Win<T>> FindWins<T>(HexGrid<T> grid, Hex hex) where T : struct =>
        grid.TryGet(hex, out _) ? [.. FindWins(grid, FindRegion(grid, hex))] : [];

    private static Win<T>? TryBridge<T>(HexGrid<T> grid, Region<T> region) where T : struct
    {
        if (region.Corners.Count < 2) { return null; }

        var corners = region.Corners.Order().Take(2).ToArray();
        var isMember = Membership(grid, region);
        var (distance, parent) = Bfs(grid, region.Members.Where(m => TouchesCorner(grid, m, corners[0])), isMember);
        var end = region.Members.First(m => TouchesCorner(grid, m, corners[1]));

        return new Win<T>(WinKind.Bridge, region.Group, [.. Trace(end, parent)]);
    }

    private static Win<T>? TryFork<T>(HexGrid<T> grid, Region<T> region) where T : struct
    {
        if (region.Edges.Count < 3) { return null; }

        var isMember = Membership(grid, region);
        var searches = region.Edges.Order().Take(3)
            .Select(edge => Bfs(grid, region.Members.Where(m => TouchesEdge(grid, m, edge)), isMember))
            .ToArray();

        var junction = region.Members.MinBy(m => searches.Sum(search => search.Distance[m.Index]));
        var cells = searches.SelectMany(search => Trace(junction, search.Parent)).Distinct().ToList();

        return new Win<T>(WinKind.Fork, region.Group, cells);
    }

    /// <summary>
    /// A region has a ring iff some cell outside it cannot reach the board's border without crossing the region.
    /// </summary>
    private static Win<T>? TryRing<T>(HexGrid<T> grid, Region<T> region) where T : struct
    {
        if (region.Members.Count < 6) { return null; }

        var isMember = Membership(grid, region);
        var outside = new bool[grid.Count];
        var pending = new Stack<Hex>();

        foreach (var hex in GridHexes(grid))
        {
            if (!isMember[hex.Index] && (grid.TryGetEdge(hex, out _) || grid.TryGetCorner(hex, out _)))
            {
                outside[hex.Index] = true;
                pending.Push(hex);
            }
        }

        while (pending.Count > 0)
        {
            foreach (var neighbor in grid.Neighbors(pending.Pop()))
            {
                if (!isMember[neighbor.Index] && !outside[neighbor.Index])
                {
                    outside[neighbor.Index] = true;
                    pending.Push(neighbor);
                }
            }
        }

        var enclosed = GridHexes(grid).Where(h => !isMember[h.Index] && !outside[h.Index]).ToArray();
        if (enclosed.Length == 0) { return null; }

        var boundary = new List<Hex>();
        var seen = new HashSet<Hex>();
        foreach (var hex in enclosed)
        {
            foreach (var neighbor in grid.Neighbors(hex))
            {
                if (isMember[neighbor.Index] && seen.Add(neighbor))
                {
                    boundary.Add(neighbor);
                }
            }
        }

        return new Win<T>(WinKind.Ring, region.Group, boundary);
    }

    private static IEnumerable<Hex> GridHexes<T>(HexGrid<T> grid) where T : struct =>
        Enumerable.Range(0, grid.Count).Select(Hex.FromIndex);

    private static bool[] Membership<T>(HexGrid<T> grid, Region<T> region) where T : struct
    {
        var isMember = new bool[grid.Count];
        foreach (var member in region.Members) { isMember[member.Index] = true; }
        return isMember;
    }

    private static bool TouchesCorner<T>(HexGrid<T> grid, Hex hex, GridCorner corner) where T : struct =>
        grid.TryGetCorner(hex, out var c) && c == corner;

    private static bool TouchesEdge<T>(HexGrid<T> grid, Hex hex, GridEdge edge) where T : struct =>
        grid.TryGetEdge(hex, out var e) && e == edge;

    /// <summary>
    /// Breadth-first search from all <paramref name="sources"/> at once, restricted to member cells.
    /// Both arrays are indexed by <see cref="Hex.Index"/>; Parent is null for sources and unreached cells.
    /// </summary>
    private static (int[] Distance, Hex?[] Parent) Bfs<T>(HexGrid<T> grid, IEnumerable<Hex> sources, bool[] isMember) where T : struct
    {
        var distance = new int[grid.Count];
        var parent = new Hex?[grid.Count];
        Array.Fill(distance, int.MaxValue);

        var pending = new Queue<Hex>();
        foreach (var source in sources)
        {
            distance[source.Index] = 0;
            pending.Enqueue(source);
        }

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var neighbor in grid.Neighbors(current))
            {
                if (isMember[neighbor.Index] && distance[neighbor.Index] == int.MaxValue)
                {
                    distance[neighbor.Index] = distance[current.Index] + 1;
                    parent[neighbor.Index] = current;
                    pending.Enqueue(neighbor);
                }
            }
        }

        return (distance, parent);
    }

    /// <summary>Follows parent links from <paramref name="cell"/> back to a BFS source.</summary>
    private static IEnumerable<Hex> Trace(Hex cell, Hex?[] parent)
    {
        for (Hex? current = cell; current is { } hex; current = parent[hex.Index])
        {
            yield return hex;
        }
    }
}
