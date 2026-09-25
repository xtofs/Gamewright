namespace Gamewright.HexBoard;

/// <summary>Generic disjoint-set structure over indices [0, n). Carries no domain-specific payload.</summary>
public sealed class UnionFind
{
    private readonly int[] _parent;
    private readonly byte[] _rank;

    public UnionFind(int n)
    {
        _parent = new int[n];
        _rank = new byte[n];

        for (var i = 0; i < n; i++)
        {
            _parent[i] = i;
        }
    }

    public int Find(int x)
    {
        while (_parent[x] != x)
        {
            x = _parent[x] = _parent[_parent[x]];
        }

        return x;
    }

    /// <summary>Unions the sets containing a and b, returning the resulting root.</summary>
    public int Union(int a, int b)
    {
        var ra = Find(a);
        var rb = Find(b);
        if (ra == rb)
        {
            return ra;
        }

        if (_rank[ra] < _rank[rb])
        {
            _parent[ra] = rb;
            return rb;
        }

        _parent[rb] = ra;
        if (_rank[ra] == _rank[rb])
        {
            _rank[ra]++;
        }

        return ra;
    }
}
