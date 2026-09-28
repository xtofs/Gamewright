namespace Gamewright.HexBoard.Graphics;

using System.Numerics;
using Gamewright.Graphics;
using Gamewright.HexBoard;

/// <summary>
/// A layout tree leaf that fits a <see cref="HexLayout"/> into its rect whenever the tree is updated.
/// Hit-testing returns this node for the whole rect; use <see cref="TryGetHex"/> for the exact hexagon.
/// </summary>
public sealed class HexBoardNode(int radius) : LeafNode
{
    public HexLayout Layout { get; } = new(radius);

    /// <inheritdoc cref="HexLayout.TryGetHex"/>
    public bool TryGetHex(Vector2 position, out Coordinate hex) => Layout.TryGetHex(position, out hex);

    protected override Rect Compute(Rect parent)
    {
        Layout.Update(parent.Position, parent.Size);
        return parent;
    }
}

public static class HexBoardNodeExtensions
{
    /// <summary>
    /// Adds a hex board of <paramref name="radius"/>, centered at its own aspect ratio so the
    /// node's rect is tight around the board.
    /// </summary>
    public static HexBoardNode AddHexBoard(this Node parent, int radius, float insetFraction = 0f)
        => parent
            .AddCenteredCanvas(HexLayout.AspectRatio(radius), insetFraction)
            .Add(new HexBoardNode(radius));
}
