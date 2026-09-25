namespace Gamewright.HexBoard;

/// <summary>
/// Represents an edge of the pointy-top hexagonal board .
/// The enum uses the 2nd order cardinal directions (NW, SE, ...) 
/// for naming the direction the edge is facing, even though they are 
/// not exactly in the 45-degree increments cardinal directions indicate.
/// </summary>
public enum EdgeDirection
{
    North = 0,
    NorthEast = 1,
    SouthEast = 2,
    South = 3,
    SouthWest = 4,
    NorthWest = 5
}
