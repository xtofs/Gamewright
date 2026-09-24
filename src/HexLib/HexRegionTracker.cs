namespace HexLib;

using System.Numerics;

/// <summary>
/// Tracks connected regions of same-group cells on a <see cref="HexGrid{T}"/>, aggregating each
/// region's edge/corner touch masks. Composed on top of a grid without the grid knowing about it:
/// callers feed it placements (group, edge mask, corner mask, neighbors) taken from the grid.
/// </summary>
public sealed class HexRegionTracker<TGroup>
{
    private readonly UnionFind _unionFind;
    private readonly ushort[] _edgeMask;
    private readonly ushort[] _cornerMask;
    private readonly TGroup?[] _group;

    public HexRegionTracker(int cellCount)
    {
        _unionFind = new UnionFind(cellCount);
        _edgeMask = new ushort[cellCount];
        _cornerMask = new ushort[cellCount];
        _group = new TGroup?[cellCount];
    }

    /// <summary>Registers a placement, unioning it with same-group cells among <paramref name="neighbors"/>.</summary>
    public void Register(int index, TGroup group, ushort edgeMask, ushort cornerMask, ReadOnlySpan<int> neighbors)
    {
        _group[index] = group;
        _edgeMask[index] = edgeMask;
        _cornerMask[index] = cornerMask;

        foreach (var neighbor in neighbors)
        {
            if (neighbor >= 0 && EqualityComparer<TGroup?>.Default.Equals(_group[neighbor], group))
            {
                Merge(index, neighbor);
            }
        }
    }

    private void Merge(int a, int b)
    {
        var ra = _unionFind.Find(a);
        var rb = _unionFind.Find(b);
        if (ra == rb)
        {
            return;
        }

        var root = _unionFind.Union(a, b);
        var absorbed = root == ra ? rb : ra;
        _edgeMask[root] |= _edgeMask[absorbed];
        _cornerMask[root] |= _cornerMask[absorbed];
    }

    public ushort EdgeMask(int index) => _edgeMask[_unionFind.Find(index)];

    public ushort CornerMask(int index) => _cornerMask[_unionFind.Find(index)];

    public bool HasFork(int index) => BitOperations.PopCount(EdgeMask(index)) >= 3;

    public bool HasBridge(int index) => BitOperations.PopCount(CornerMask(index)) >= 2;
}
