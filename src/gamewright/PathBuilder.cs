namespace Gamewright;

public sealed class PathBuilder
{
    private readonly List<byte> pathData = [];
    private readonly List<float> geometry = [];
    private bool startsWithMove;
    private bool closed;

    public PathBuilder MoveTo(float x, float y)
    {
        Add(PathOpcode.MoveTo, x, y);
        startsWithMove = true;
        closed = false;
        return this;
    }

    public PathBuilder LineTo(float x, float y)
    {
        Add(PathOpcode.LineTo, x, y);
        return this;
    }

    public PathBuilder QuadTo(float controlX, float controlY, float x, float y)
    {
        Add(PathOpcode.QuadTo, controlX, controlY, x, y);
        return this;
    }

    public PathBuilder CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y)
    {
        Add(PathOpcode.CubicTo, control1X, control1Y, control2X, control2Y, x, y);
        return this;
    }

    public PathBuilder ArcTo(
        float radiusX,
        float radiusY,
        float rotation,
        bool largeArc,
        bool sweep,
        float x,
        float y)
    {
        Add(PathOpcode.ArcTo, radiusX, radiusY, rotation, largeArc ? 1 : 0, sweep ? 1 : 0, x, y);
        return this;
    }

    public PathBuilder Close()
    {
        EnsureStarted();
        pathData.Add((byte)PathOpcode.ClosePath);
        closed = true;
        return this;
    }

    internal (byte[] PathData, float[] Geometry, bool Closed) Build()
    {
        EnsureStarted();
        if (pathData.Count == 0)
            throw new InvalidOperationException("A path must contain at least one operation.");
        return ([.. pathData], [.. geometry], closed);
    }

    private void Add(PathOpcode opcode, params float[] values)
    {
        if (opcode != PathOpcode.MoveTo)
            EnsureStarted();
        if (Array.Exists(values, static value => !float.IsFinite(value)))
            throw new ArgumentOutOfRangeException(nameof(values), "Path coordinates must be finite.");

        pathData.Add((byte)opcode);
        geometry.AddRange(values);
        if (opcode != PathOpcode.ClosePath)
            closed = false;
    }

    private void EnsureStarted()
    {
        if (!startsWithMove)
            throw new InvalidOperationException("A path must begin with MoveTo.");
    }
}
