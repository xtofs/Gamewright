namespace Gamewright.HexBoard;

using System.Numerics;

/// <summary>
/// Finds connected regions of same-group cells on a <see cref="HexGrid{T}"/>, aggregating each
/// region's edge/corner touch masks from the current grid contents.
/// </summary>
public sealed class HexRegionTracker<TGroup>(HexGrid<TGroup> grid) where TGroup : struct
{
    private readonly HexGrid<TGroup> _grid = grid;

    private Region<TGroup> FindRegion(int index)
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

internal record struct Region<TGroup>(TGroup Group, ushort Edges, ushort Corners, List<int> Members) where TGroup : struct
{
    public static implicit operator (TGroup Group, ushort Edges, ushort Corners, List<int> Members)(Region<TGroup> value)
    {
        return (value.Group, value.Edges, value.Corners, value.Members);
    }

    public static implicit operator Region<TGroup>((TGroup Group, ushort Edges, ushort Corners, List<int> Members) value)
    {
        return new Region<TGroup>(value.Group, value.Edges, value.Corners, value.Members);
    }
}
