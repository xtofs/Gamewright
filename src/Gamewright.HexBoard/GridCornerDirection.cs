namespace Gamewright.HexBoard;

/// <summary>
/// Represents the corners of a pointy-top hexagonal board.
/// The enum uses the 2nd order cardinal directions (NW, SE, ...) 
/// for naming the direction the corner is facing, even though they are 
/// not exactly in the 45-degree increments cardinal directions indicate.
/// </summary>
/// <note>
/// These are slightly different from the EdgeDirection enum, there is
/// no North and South corner on a grid with pointy-top hexagons.
/// 
/// </note>
public enum GridCornerDirection
{
    NorthWest, NorthEast, East, SouthEast, SouthWest, West
}
