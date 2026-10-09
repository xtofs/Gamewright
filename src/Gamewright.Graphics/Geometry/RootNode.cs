namespace Gamewright.Graphics;

using System.Numerics;

/// <summary>Root of a layout tree. Its rect equals the framebuffer size passed to <see cref="Update"/>.</summary>
public sealed class RootNode : SingleChildNode
{
    protected override Rect Layout(Rect parent) => parent;

    /// <summary>Recomputes the entire tree for the given framebuffer size.</summary>
    public void Update(Vector2 framebufferSize) => Recompute(framebufferSize.ToRect());

    /// <summary>Returns the deepest hit-testable node under <paramref name="point"/>, or false if none.</summary>
    public new bool TryHitTest(Vector2 point, out Node? node) => base.TryHitTest(point, out node);
}
