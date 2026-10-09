namespace Gamewright.SquareBoard.Graphics;

using Gamewright.Graphics;

public static class CheckerboardNodeExtensions
{
    /// <summary>
    /// Adds a board of <paramref name="files"/> × <paramref name="ranks"/> squares, centered at
    /// its own aspect ratio so the squares stay square.
    /// </summary>
    public static CheckerboardNode AddCheckerboard(
        this Node parent, int files, int ranks, bool withLabels = true, float insetFraction = 0f,
        Color? light = null, Color? dark = null)
        => parent
            .AddCenteredCanvas(CheckerboardNode.AspectRatio(files, ranks, withLabels), insetFraction)
            .Add(new CheckerboardNode(files, ranks, withLabels, light, dark));
}
