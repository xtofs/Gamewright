namespace Gamewright.HexagonalGrid;

using System.Numerics;




public enum Neighbor
{
    OneOClock,
    ThreeOClock,
    FiveOClock,
    SevenOClock,
    NineOClock,
    ElevenOClock
}

/// <summary>
/// bitset for the neighboring hex cells in a pointy-top hexagonal grid
/// </summary>
[Flags]
public enum NeighborSet
{
    OneOClock = 1 << Neighbor.OneOClock,
    ThreeOClock = 1 << Neighbor.ThreeOClock,
    FiveOClock = 1 << Neighbor.FiveOClock,
    SevenOClock = 1 << Neighbor.SevenOClock,
    NineOClock = 1 << Neighbor.NineOClock,
    ElevenOClock = 1 << Neighbor.ElevenOClock,
}

public static class NeighborSetExtensions
{
    private const NeighborSet AllNeighbors =
        NeighborSet.OneOClock |
        NeighborSet.ThreeOClock |
        NeighborSet.FiveOClock |
        NeighborSet.SevenOClock |
        NeighborSet.NineOClock |
        NeighborSet.ElevenOClock;

    extension(Neighbor)
    {
        public static IReadOnlyList<Neighbor> All =>
        [
            Neighbor.OneOClock,
            Neighbor.ThreeOClock,
            Neighbor.FiveOClock,
            Neighbor.SevenOClock,
            Neighbor.NineOClock,
            Neighbor.ElevenOClock
        ];
    }

    extension(NeighborSet)
    {
        public static NeighborSet AllDirections => AllNeighbors;
    }

    extension(Neighbor neighbor)
    {
        public NeighborSet ToSingletonSet() => (NeighborSet)(1 << (int)neighbor);

        public HexOffset Direction => neighbor switch
        {
            Neighbor.OneOClock => new HexOffset(1, -1, 0),
            Neighbor.ThreeOClock => new HexOffset(1, 0, -1),
            Neighbor.FiveOClock => new HexOffset(0, 1, -1),
            Neighbor.SevenOClock => new HexOffset(-1, 1, 0),
            Neighbor.NineOClock => new HexOffset(-1, 0, 1),
            Neighbor.ElevenOClock => new HexOffset(0, -1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(neighbor), "Invalid hex neighbor")
        };
    }

    extension(NeighborSet set)
    {
        public bool Has(Neighbor neighbor) => (set & neighbor.ToSingletonSet()) == neighbor.ToSingletonSet();

        public NeighborSet Inverse => ~set & AllNeighbors;


        public bool TryGetCorner(out GridCorner corner)
        {
            switch (set.Inverse)
            {
                case NeighborSet.NineOClock | NeighborSet.ElevenOClock | NeighborSet.OneOClock:
                    corner = GridCorner.ElevenOClock;
                    return true;
                case NeighborSet.ElevenOClock | NeighborSet.OneOClock | NeighborSet.ThreeOClock:
                    corner = GridCorner.OneOClock;
                    return true;
                case NeighborSet.OneOClock | NeighborSet.ThreeOClock | NeighborSet.FiveOClock:
                    corner = GridCorner.ThreeOClock;
                    return true;
                case NeighborSet.ThreeOClock | NeighborSet.FiveOClock | NeighborSet.SevenOClock:
                    corner = GridCorner.FiveOClock;
                    return true;
                case NeighborSet.FiveOClock | NeighborSet.SevenOClock | NeighborSet.NineOClock:
                    corner = GridCorner.SevenOClock;
                    return true;
                case NeighborSet.SevenOClock | NeighborSet.NineOClock | NeighborSet.ElevenOClock:
                    corner = GridCorner.NineOClock;
                    return true;
                default:
                    corner = default;
                    return false;
            }
        }

        public bool TryGetEdge(out GridEdge edge)
        {
            switch (set.Inverse)
            {
                case NeighborSet.ElevenOClock | NeighborSet.OneOClock:
                    edge = GridEdge.TwelveOClock;
                    return true;
                case NeighborSet.OneOClock | NeighborSet.ThreeOClock:
                    edge = GridEdge.TwoOClock;
                    return true;
                case NeighborSet.ThreeOClock | NeighborSet.FiveOClock:
                    edge = GridEdge.FourOClock;
                    return true;
                case NeighborSet.FiveOClock | NeighborSet.SevenOClock:
                    edge = GridEdge.SixOClock;
                    return true;
                case NeighborSet.SevenOClock | NeighborSet.NineOClock:
                    edge = GridEdge.EightOClock;
                    return true;
                case NeighborSet.NineOClock | NeighborSet.ElevenOClock:
                    edge = GridEdge.TenOClock;
                    return true;
                default:
                    edge = default;
                    return false;
            }
        }

        public NeighborSet Add(Neighbor neighbor) => set | neighbor.ToSingletonSet();

        public NeighborSet Remove(Neighbor neighbor) => set & ~neighbor.ToSingletonSet();

        public bool TryGetNext(out Neighbor neighbor)
        {
            var remaining = (int)(set & AllNeighbors);
            if (remaining == 0)
            {
                neighbor = default;
                return false;
            }

            neighbor = (Neighbor)BitOperations.TrailingZeroCount((uint)remaining);
            return true;
        }

    }
}
