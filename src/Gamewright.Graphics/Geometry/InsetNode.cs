namespace Gamewright.Graphics;

/// <summary>Wraps a single child, shrunk by a fixed pixel amount on all sides.</summary>
public sealed class InsetNode : SingleChildNode
{
    private readonly float _amount;

    internal InsetNode(float amount) => _amount = amount;

    protected override Rect Layout(Rect parent) => parent.Inset(_amount);
}
