namespace Gamewright;

public sealed class FrameBuilder
{
    private readonly List<DrawCommand> commands = [];
    private readonly List<byte> pathData = [];
    private readonly List<float> geometry = [];
    private readonly List<Style> styles = [];
    private readonly List<string> strings = [];

    public FrameBuilder AddPath(
        string path,
        Color? fillColor = null,
        StrokeStyle? strokeStyle = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var parsed = SvgPathParser.Parse(path);
        AddPath(parsed.PathData, parsed.Geometry, fillColor, strokeStyle, parsed.Closed);
        return this;
    }

    public FrameBuilder AddPath(
        Action<PathBuilder> buildPath,
        Color? fillColor = null,
        StrokeStyle? strokeStyle = null)
    {
        ArgumentNullException.ThrowIfNull(buildPath);
        var path = new PathBuilder();
        buildPath(path);
        var result = path.Build();
        AddPath(result.PathData, result.Geometry, fillColor, strokeStyle, result.Closed);
        return this;
    }

    private void AddPath(
        byte[] operations,
        float[] pathGeometry,
        Color? fillColor,
        StrokeStyle? strokeStyle,
        bool closed)
    {
        if (fillColor is null && strokeStyle is null)
            throw new ArgumentException("A path must have a fill or stroke style.");
        if (strokeStyle is { Width: <= 0 } || strokeStyle is { Width: var width } && !float.IsFinite(width))
            throw new ArgumentOutOfRangeException(nameof(strokeStyle), "Stroke width must be finite and positive.");

        var pathDataOffset = pathData.Count;
        var geometryOffset = geometry.Count;
        pathData.AddRange(operations);
        AddGeometry(pathGeometry);
        var styleIndex = AddStyle(new Style(
            fillColor?.Rgba ?? 0,
            strokeStyle?.Color.Rgba ?? 0,
            strokeStyle?.Width ?? 0,
            (int)FontIndex.Body));
        var flags = (fillColor is null ? DrawCommandFlags.None : DrawCommandFlags.Fill)
            | (strokeStyle is null ? DrawCommandFlags.None : DrawCommandFlags.Stroke)
            | (closed ? DrawCommandFlags.Closed : DrawCommandFlags.None);
        commands.Add(new DrawCommand(
            DrawCommandType.Path,
            styleIndex,
            (ushort)flags,
            geometryOffset,
            pathGeometry.Length,
            pathDataOffset,
            operations.Length,
            0));
    }

    public FrameBuilder AddSprite(int spriteIndex, float x, float y, float width, float height)
    {
        if (spriteIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(spriteIndex));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Sprite dimensions must be positive.");

        ValidateGeometry([x, y, width, height]);
        var geometryOffset = AddGeometry([x, y, width, height]);
        var styleIndex = AddStyle(default);
        commands.Add(new DrawCommand(DrawCommandType.Image, styleIndex, 0, geometryOffset, 4, 0, 0, spriteIndex));
        return this;
    }

    public FrameBuilder AddText(
        string text,
        float x,
        float y,
        Color fillColor,
        FontIndex font = FontIndex.Body,
        TextAlignment alignment = TextAlignment.Start)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!Enum.IsDefined(font))
            throw new ArgumentOutOfRangeException(nameof(font));

        ValidateGeometry([x, y]);
        var stringIndex = AddString(text);
        var geometryOffset = AddGeometry([x, y]);
        var styleIndex = AddStyle(new Style(fillColor.Rgba, 0, 0, (int)font));
        var flags = alignment switch
        {
            TextAlignment.Start => DrawCommandFlags.None,
            TextAlignment.Center => DrawCommandFlags.AlignCenter,
            TextAlignment.End => DrawCommandFlags.AlignEnd,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment))
        };
        commands.Add(new DrawCommand(
            DrawCommandType.Text,
            styleIndex,
            (ushort)flags,
            geometryOffset,
            2,
            0,
            0,
            stringIndex));
        return this;
    }

    public Frame Build() => new Frame([.. commands], [.. pathData], [.. geometry], [.. styles], [.. strings]);

    private int AddGeometry(ReadOnlySpan<float> values)
    {
        var offset = geometry.Count;
        foreach (var value in values)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(values), "Geometry values must be finite.");
            geometry.Add(value);
        }
        return offset;
    }

    private static void ValidateGeometry(ReadOnlySpan<float> values)
    {
        foreach (var value in values)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(values), "Geometry values must be finite.");
        }
    }

    private ushort AddStyle(Style style)
    {
        if (styles.Count >= ushort.MaxValue)
            throw new InvalidOperationException("A frame cannot contain more than 65,535 styles.");

        styles.Add(style);
        return (ushort)(styles.Count - 1);
    }

    private int AddString(string value)
    {
        var index = strings.Count;
        strings.Add(value);
        return index;
    }
}
