namespace Gamewright.HexBoard;

/// <summary>
/// The six directions from a pointy-top hexagon to its neighbors.
/// </summary>
/// <remarks>
/// The order matches <see cref="CornerDirection"/>: the corner of a grid with radius R
/// in direction d is R * <see cref="CubeCoord.Offset(HexDirection)"/>.
/// Unlike <see cref="EdgeDirection"/>, which names the edges of the board,
/// these name the directions between adjacent cells.
/// </remarks>
public enum HexDirection
{
    NorthWest, NorthEast, East, SouthEast, SouthWest, West
}

public static class HexDirectionExtensions
{
    extension(HexDirection direction)
    {
        public HexDirection Opposite() => (HexDirection)(((int)direction + 3) % 6);
    }
}
