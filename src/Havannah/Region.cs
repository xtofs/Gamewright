namespace Havannah;

/// <summary>
/// A connected region of same-group hexes, together with the board edges and corners it touches.
/// </summary>
/// <typeparam name="T">The kind of piece, which is also the group (player).</typeparam>
/// <param name="Group">The group (player) of this region.</param>
/// <param name="Edges">The board edges the region touches.</param>
/// <param name="Corners">The board corners the region touches.</param>
/// <param name="Members">The hexes of the region.</param>
public readonly record struct Region<T>(T Group, HashSet<GridEdge> Edges, HashSet<GridCorner> Corners, List<Hex> Members)
    where T : struct;
