namespace Gamewright;

public sealed class FrameBuilder
{
    private readonly List<DrawCommand> commands = [];
    private readonly List<byte> pathData = [];
    private readonly List<float> geometry = [];
    private readonly List<Paint> paints = [];
    private readonly Dictionary<Paint, ushort> paintIndices = [];
    private readonly List<TextStyle> textStyles = [];
    private readonly Dictionary<TextStyle, ushort> textStyleIndices = [];
    private readonly List<string> strings = [];
    private readonly List<string> images = [];

    public FrameBuilder AddPath(string path, Paint paint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RequireDrawable(paint);

        var parsed = SvgPathParser.Parse(path);
        AddPath(parsed.PathData, parsed.Geometry, paint, parsed.Closed);
        return this;
    }

    public FrameBuilder AddPath(Action<PathBuilder> buildPath, Paint paint)
    {
        ArgumentNullException.ThrowIfNull(buildPath);
        RequireDrawable(paint);

        var path = new PathBuilder();
        buildPath(path);
        var result = path.Build();
        AddPath(result.PathData, result.Geometry, paint, result.Closed);
        return this;
    }

    private void AddPath(byte[] operations, float[] pathGeometry, Paint paint, bool closed)
    {
        var pathDataOffset = pathData.Count;
        var geometryOffset = geometry.Count;
        pathData.AddRange(operations);
        AddGeometry(pathGeometry);
        commands.Add(new DrawCommand(
            DrawCommandType.Path,
            AddStyle(paints, paintIndices, paint),
            (ushort)(closed ? DrawCommandFlags.Closed : DrawCommandFlags.None),
            geometryOffset,
            pathGeometry.Length,
            pathDataOffset,
            operations.Length,
            0));
    }

    private static void RequireDrawable(Paint paint)
    {
        if (paint.IsEmpty)
            throw new ArgumentException("A path must have a fill or a stroke.", nameof(paint));
    }

    /// <summary>Draws the <paramref name="source"/> region of an image (in image pixels) scaled into <paramref name="destination"/>.</summary>
    public FrameBuilder AddImage(Image image, Rect source, Rect destination)
    {
        RequirePositive(source, nameof(source));
        RequirePositive(destination, nameof(destination));
        float[] values =
            [source.X, source.Y, source.Width, source.Height,
             destination.X, destination.Y, destination.Width, destination.Height];
        ValidateGeometry(values);

        var geometryOffset = AddGeometry(values);
        commands.Add(new DrawCommand(
            DrawCommandType.Image, 0, 0, geometryOffset, values.Length, 0, 0, AddImageName(image.Path)));
        return this;
    }

    private static void RequirePositive(Rect rect, string name)
    {
        if (!(rect.Width > 0) || !(rect.Height > 0))
            throw new ArgumentOutOfRangeException(name, "Rectangle dimensions must be positive.");
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
        var styleIndex = AddStyle(textStyles, textStyleIndices, new TextStyle(fillColor.Rgba, (int)font));
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

    public Frame Build() =>
        new Frame([.. commands], [.. pathData], [.. geometry], [.. paints], [.. textStyles], [.. strings], [.. images]);

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

    private static ushort AddStyle<T>(List<T> table, Dictionary<T, ushort> indices, T style)
        where T : notnull
    {
        if (indices.TryGetValue(style, out var existing))
            return existing;
        if (table.Count >= ushort.MaxValue)
            throw new InvalidOperationException("A frame cannot contain more than 65,535 styles of one kind.");

        table.Add(style);
        var index = (ushort)(table.Count - 1);
        indices[style] = index;
        return index;
    }

    private int AddImageName(string path)
    {
        var index = images.IndexOf(path);
        if (index >= 0)
            return index;
        images.Add(path);
        return images.Count - 1;
    }

    private int AddString(string value)
    {
        var index = strings.Count;
        strings.Add(value);
        return index;
    }
}
