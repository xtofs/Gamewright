namespace Gamewright.HexBoard;

using System.Numerics;

/// <summary>
/// Finds connected regions of same-group cells on a <see cref="HexGrid{T}"/>, aggregating each
/// region's edge/corner touch masks from the current grid contents.
/// </summary>
public sealed class HexRegionTracker<TGroup>(HexGrid<TGroup> grid) where TGroup : struct
{
    private readonly HexGrid<TGroup> _grid = grid;

    public Region<TGroup> FindRegion(int index)
    {
        if (!_grid.TryGet(CubeCoord.FromIndex(index), out var group))
        {
            throw new InvalidOperationException($"Cell {index} is not occupied.");
        }

        var visited = new bool[_grid.Count];
        var pending = new Stack<int>();
        var members = new List<int>();
        ushort edges = 0;
        ushort corners = 0;
        visited[index] = true;
        pending.Push(index);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            members.Add(current);
            edges |= _grid.EdgeMask(current);
            corners |= _grid.CornerMask(current);

            foreach (var neighbor in _grid.Neighbors(current))
            {
                if (visited[neighbor])
                {
                    continue;
                }

                visited[neighbor] = true;
                if (_grid.TryGet(CubeCoord.FromIndex(neighbor), out var piece)
                    && EqualityComparer<TGroup>.Default.Equals(piece, group))
                {
                    pending.Push(neighbor);
                }
            }
        }

        return (group, edges, corners, members);
    }

    public ushort EdgeMask(int index) => FindRegion(index).Edges;

    public ushort CornerMask(int index) => FindRegion(index).Corners;

    public bool HasFork(int index) => BitOperations.PopCount(EdgeMask(index)) >= 3;

    public bool HasBridge(int index) => BitOperations.PopCount(CornerMask(index)) >= 2;

    /// <summary>
    /// Returns a shortest chain of cells within the region of <paramref name="index"/> that connects
    /// two of the corners it touches, ordered from one corner cell to the other.
    /// </summary>
    public IReadOnlyList<int> FindBridgePath(int index)
    {
        var region = FindRegion(index);
        if (BitOperations.PopCount(region.Corners) < 2)
        {
            throw new InvalidOperationException($"The region of cell {index} does not form a bridge.");
        }

        var inRegion = ToMembership(region.Members);
        var corners = SetBits(region.Corners).Take(2).ToArray();
        var start = region.Members.First(m => (_grid.CornerMask(m) & corners[0]) != 0);
        var end = region.Members.First(m => (_grid.CornerMask(m) & corners[1]) != 0);

        var (_, parent) = Bfs([start], inRegion);
        return TracePath(end, parent).Reverse().ToList();
    }

    /// <summary>
    /// Returns three chains of cells within the region of <paramref name="index"/> that connect three
    /// of the edges it touches. Each chain starts at a common junction cell and ends on its edge,
    /// and the total length of the chains is minimal.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<int>> FindForkPaths(int index)
    {
        var region = FindRegion(index);
        if (BitOperations.PopCount(region.Edges) < 3)
        {
            throw new InvalidOperationException($"The region of cell {index} does not form a fork.");
        }

        var inRegion = ToMembership(region.Members);
        var searches = SetBits(region.Edges).Take(3)
            .Select(bit => Bfs(region.Members.Where(m => (_grid.EdgeMask(m) & bit) != 0), inRegion))
            .ToArray();

        var junction = region.Members.MinBy(m => searches.Sum(search => search.Distance[m]));
        return [.. searches.Select(search => (IReadOnlyList<int>)TracePath(junction, search.Parent).ToList())];
    }

    private bool[] ToMembership(List<int> members)
    {
        var inRegion = new bool[_grid.Count];
        foreach (var member in members)
        {
            inRegion[member] = true;
        }
        return inRegion;
    }

    /// <summary>
    /// Breadth-first search from all <paramref name="sources"/> at once, restricted to cells in
    /// <paramref name="inRegion"/>. Parent is -1 for sources and unreached cells.
    /// </summary>
    private (int[] Distance, int[] Parent) Bfs(IEnumerable<int> sources, bool[] inRegion)
    {
        var distance = new int[_grid.Count];
        var parent = new int[_grid.Count];
        Array.Fill(distance, int.MaxValue);
        Array.Fill(parent, -1);

        var pending = new Queue<int>();
        foreach (var source in sources)
        {
            distance[source] = 0;
            pending.Enqueue(source);
        }

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var neighbor in _grid.Neighbors(current))
            {
                if (inRegion[neighbor] && distance[neighbor] == int.MaxValue)
                {
                    distance[neighbor] = distance[current] + 1;
                    parent[neighbor] = current;
                    pending.Enqueue(neighbor);
                }
            }
        }

        return (distance, parent);
    }

    /// <summary>Follows <paramref name="parent"/> links from <paramref name="cell"/> back to a BFS source.</summary>
    private static IEnumerable<int> TracePath(int cell, int[] parent)
    {
        for (var current = cell; current != -1; current = parent[current])
        {
            yield return current;
        }
    }

    /// <summary>Returns each set bit of <paramref name="mask"/> as its own mask, lowest first.</summary>
    private static IEnumerable<ushort> SetBits(ushort mask)
    {
        for (var bit = 0; bit < 16; bit++)
        {
            if ((mask & (1 << bit)) != 0)
            {
                yield return (ushort)(1 << bit);
            }
        }
    }

    public string FormatRegion(int index)
    {
        var region = FindRegion(index);
        var members = region.Members.Order()
            .Select(member =>
            {
                var coord = CubeCoord.FromIndex(member);
                return $"  {member} ({coord.Q},{coord.R},{coord.S}): group={region.Group}, edges=0x{_grid.EdgeMask(member):X2}, corners=0x{_grid.CornerMask(member):X2}";
            });

        return $"Region {index}: edges=0x{region.Edges:X2}, corners=0x{region.Corners:X2}, bridge={BitOperations.PopCount(region.Corners) >= 2}, fork={BitOperations.PopCount(region.Edges) >= 3}\n{string.Join("\n", members)}";
    }
}
