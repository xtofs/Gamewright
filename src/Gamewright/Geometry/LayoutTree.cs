namespace Gamewright;

using System.Numerics;

public abstract class Node
{
    public Rect Rect { get; private set; }

    protected virtual bool IsHitTarget => false;

    public virtual IReadOnlyList<Node> Children => (IReadOnlyList<Node>?)_children ?? [];
    private List<Node>? _children;

    internal void AddChild(Node child) => OnAddChild(child);
    protected virtual void OnAddChild(Node child) => (_children ??= new()).Add(child);

    internal void Recompute(Rect parentRect)
    {
        Rect = Compute(parentRect);
        RecomputeChildren();
    }

    protected abstract Rect Compute(Rect parent);

    protected virtual void RecomputeChildren()
    {
        foreach (var child in Children)
        {
            child.Recompute(Rect);
        }
    }

    internal bool TryHitTest(Vector2 point, out Node? node)
    {
        if (!Contains(point)) { node = null; return false; }
        foreach (var child in Children)
        {
            if (child.TryHitTest(point, out node))
            {
                return true;
            }
        }

        if (IsHitTarget) { node = this; return true; }
        node = null;
        return false;
    }

    private bool Contains(Vector2 point)
        => point.X >= Rect.Left && point.X < Rect.Right
        && point.Y >= Rect.Top && point.Y < Rect.Bottom;
}

/// <summary>A node that wraps exactly one child, passing its computed rect down to it.</summary>
public abstract class SingleChildNode : Node
{
    public Node? Child { get; private set; }

    public override IReadOnlyList<Node> Children => Child is null ? [] : [Child];

    protected override void OnAddChild(Node child)
    {
        if (Child is not null)
        {
            throw new InvalidOperationException($"{GetType().Name} already has a child.");
        }

        Child = child;
    }

    protected override void RecomputeChildren() => Child?.Recompute(Rect);
}

/// <summary>A node that holds no children and is always a hit-test target.</summary>
public abstract class LeafNode : Node
{
    protected override bool IsHitTarget => true;

    public override IReadOnlyList<Node> Children => [];

    protected override void OnAddChild(Node child)
        => throw new InvalidOperationException($"{GetType().Name} cannot have children.");

    protected override void RecomputeChildren() { }
}

public static class NodeExtensions
{
    public static AspectCanvasNode AddCenteredCanvas(this Node parent, float ratio = 1f, float insetFraction = 0f)
    {
        var node = new AspectCanvasNode(ratio, insetFraction);
        parent.AddChild(node);
        return node;
    }

    public static InsetNode AddInset(this Node parent, float amount)
    {
        var node = new InsetNode(amount);
        parent.AddChild(node);
        return node;
    }

    public static Grid AddUniformGrid(this Node parent, int cols, int rows)
    {
        var colDefs = Enumerable.Repeat(new ColumnDefinition(new GridLength(1, GridLengthUnit.Proportional)), cols).ToArray();
        var rowDefs = Enumerable.Repeat(new RowDefinition(new GridLength(1, GridLengthUnit.Proportional)), rows).ToArray();
        var node = new Grid(colDefs, rowDefs);
        parent.AddChild(node);
        return node;
    }

    public static Grid AddGrid(this Node parent, ColumnDefinition[] columns, RowDefinition[] rows)
    {
        var node = new Grid(columns, rows);
        parent.AddChild(node);
        return node;
    }
}
