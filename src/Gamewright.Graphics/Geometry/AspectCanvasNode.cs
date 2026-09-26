namespace Gamewright.Graphics;

/// <summary>
/// Wraps a single child, centered within the parent at a fixed aspect ratio,
/// optionally inset by a fraction of the shorter side.
/// </summary>
public sealed class AspectCanvasNode : SingleChildNode
{
    private readonly float _ratio;
    private readonly float _insetFraction;

    internal AspectCanvasNode(float ratio, float insetFraction)
    {
        _ratio = ratio;
        _insetFraction = insetFraction;
    }

    protected override Rect Compute(Rect parent)
    {
        var scale = MathF.Min(parent.Width, parent.Height);
        return parent.Inset(scale * _insetFraction).GetMaxOfRatio(_ratio);
    }
}
