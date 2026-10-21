namespace Gamewright;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public enum DrawCommandType : ushort
{
    Path = 1,
    Text = 2,
    Image = 3
}

public enum PathOpcode : byte
{
    MoveTo,
    LineTo,
    QuadTo,
    CubicTo,
    ArcTo,
    ClosePath
}

[Flags]
public enum DrawCommandFlags : ushort
{
    None = 0,
    Fill = 1,
    Stroke = 2,
    Closed = 4,
    AlignCenter = 8,
    AlignEnd = 16
}

public enum FontIndex
{
    Body,
    Heading
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DrawCommand(
    DrawCommandType Type,
    ushort StyleIndex,
    ushort Flags,
    int GeometryOffset,
    int GeometryLength,
    int PathDataOffset,
    int PathDataLength,
    int Data);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Style(
    uint FillColor,
    uint StrokeColor,
    float StrokeWidth,
    int FontIndex);

public sealed record RenderFrame(
    DrawCommand[] Commands,
    byte[] PathData,
    float[] Geometry,
    Style[] Styles,
    string[] Strings);

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
        if (fillColor is null && strokeStyle is null)
            throw new ArgumentException("A path must have a fill or stroke style.", nameof(path));
        if (strokeStyle is { Width: <= 0 })
            throw new ArgumentOutOfRangeException(nameof(strokeStyle), "Stroke width must be positive.");

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

    public FrameBuilder AddImage(int imageId, float x, float y, float width, float height)
    {
        if (imageId < 0)
            throw new ArgumentOutOfRangeException(nameof(imageId));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be positive.");

        ValidateGeometry(x, y, width, height);
        var geometryOffset = AddGeometry([x, y, width, height]);
        var styleIndex = AddStyle(default);
        commands.Add(new DrawCommand(DrawCommandType.Image, styleIndex, 0, geometryOffset, 4, 0, 0, imageId));
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

        ValidateGeometry(x, y);
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

    public RenderFrame Build() => new([.. commands], [.. pathData], [.. geometry], [.. styles], [.. strings]);

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

    private static void ValidateGeometry(params float[] values)
    {
        if (Array.Exists(values, static value => !float.IsFinite(value)))
            throw new ArgumentOutOfRangeException(nameof(values), "Geometry values must be finite.");
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

public enum TextAlignment
{
    Start,
    Center,
    End
}

public readonly record struct Color(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public uint Rgba => (uint)(R | (G << 8) | (B << 16) | (A << 24));
}

public readonly record struct StrokeStyle(Color Color, float Width);

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

internal static class SvgPathParser
{
    private static readonly Regex TokenPattern = new(
        @"[a-zA-Z]|[-+]?(?:\d*\.?\d+)(?:[eE][-+]?\d+)?",
        RegexOptions.CultureInvariant);

    public static (byte[] PathData, float[] Geometry, bool Closed) Parse(string path)
    {
        var matches = TokenPattern.Matches(path);
        var previousEnd = 0;
        foreach (Match match in matches)
        {
            if (path.AsSpan(previousEnd, match.Index - previousEnd)
                .ContainsAnyExcept(" \t\r\n,"))
            {
                throw new FormatException("SVG path data contains an invalid token.");
            }
            previousEnd = match.Index + match.Length;
        }
        if (path.AsSpan(previousEnd).ContainsAnyExcept(" \t\r\n,"))
            throw new FormatException("SVG path data contains an invalid token.");

        var tokens = matches.Select(match => match.Value).ToArray();
        if (tokens.Length == 0 || tokens[0] is not ("M" or "m"))
            throw new FormatException("SVG path data must begin with a move command.");
        var pathData = new List<byte>();
        var geometry = new List<float>();
        var index = 0;
        var command = '\0';
        var currentX = 0f;
        var currentY = 0f;
        var subpathX = 0f;
        var subpathY = 0f;
        var previousControlX = 0f;
        var previousControlY = 0f;
        var previousCommand = '\0';
        var closed = false;

        while (index < tokens.Length)
        {
            if (IsCommand(tokens[index]))
            {
                command = tokens[index++][0];
                if (command is 'Z' or 'z')
                {
                    pathData.Add((byte)PathOpcode.ClosePath);
                    currentX = subpathX;
                    currentY = subpathY;
                    previousCommand = command;
                    closed = true;
                    command = '\0';
                    continue;
                }
            }
            else if (command == '\0')
            {
                throw new FormatException("SVG path data must begin with a command.");
            }

            if (char.ToUpperInvariant(command) != 'Z')
                closed = false;

            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    {
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        pathData.Add((byte)PathOpcode.MoveTo);
                        geometry.Add(x);
                        geometry.Add(y);
                        currentX = subpathX = x;
                        currentY = subpathY = y;
                        previousCommand = command;
                        command = relative ? 'l' : 'L';
                        break;
                    }
                case 'L':
                    {
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddLine(pathData, geometry, x, y);
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'H':
                    {
                        var x = ReadNumber(tokens, ref index);
                        if (relative) x += currentX;
                        AddLine(pathData, geometry, x, currentY);
                        currentX = x;
                        previousCommand = command;
                        break;
                    }
                case 'V':
                    {
                        var y = ReadNumber(tokens, ref index);
                        if (relative) y += currentY;
                        AddLine(pathData, geometry, currentX, y);
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'C':
                    {
                        var (x1, y1) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        var (x2, y2) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddCubic(pathData, geometry, x1, y1, x2, y2, x, y);
                        previousControlX = x2;
                        previousControlY = y2;
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'S':
                    {
                        var x1 = char.ToUpperInvariant(previousCommand) is 'C' or 'S'
                            ? 2 * currentX - previousControlX
                            : currentX;
                        var y1 = char.ToUpperInvariant(previousCommand) is 'C' or 'S'
                            ? 2 * currentY - previousControlY
                            : currentY;
                        var (x2, y2) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddCubic(pathData, geometry, x1, y1, x2, y2, x, y);
                        previousControlX = x2;
                        previousControlY = y2;
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'Q':
                    {
                        var (controlX, controlY) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddQuadratic(pathData, geometry, controlX, controlY, x, y);
                        previousControlX = controlX;
                        previousControlY = controlY;
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'T':
                    {
                        var controlX = char.ToUpperInvariant(previousCommand) is 'Q' or 'T'
                            ? 2 * currentX - previousControlX
                            : currentX;
                        var controlY = char.ToUpperInvariant(previousCommand) is 'Q' or 'T'
                            ? 2 * currentY - previousControlY
                            : currentY;
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddQuadratic(pathData, geometry, controlX, controlY, x, y);
                        previousControlX = controlX;
                        previousControlY = controlY;
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                case 'A':
                    {
                        var radiusX = ReadNumber(tokens, ref index);
                        var radiusY = ReadNumber(tokens, ref index);
                        var rotation = ReadNumber(tokens, ref index);
                        var largeArc = ReadFlag(tokens, ref index);
                        var sweep = ReadFlag(tokens, ref index);
                        var (x, y) = ReadPoint(tokens, ref index, relative, currentX, currentY);
                        AddArc(pathData, geometry, currentX, currentY, radiusX, radiusY, rotation, largeArc, sweep, x, y);
                        currentX = x;
                        currentY = y;
                        previousCommand = command;
                        break;
                    }
                default:
                    throw new FormatException($"Unsupported SVG path command '{command}'.");
            }
        }

        if (pathData.Count == 0)
            throw new FormatException("SVG path data contains no drawing commands.");
        return ([.. pathData], [.. geometry], closed);
    }

    private static void AddLine(List<byte> pathData, List<float> geometry, float x, float y)
    {
        pathData.Add((byte)PathOpcode.LineTo);
        geometry.Add(x);
        geometry.Add(y);
    }

    private static void AddCubic(List<byte> pathData, List<float> geometry, float x1, float y1, float x2, float y2, float x, float y)
    {
        pathData.Add((byte)PathOpcode.CubicTo);
        geometry.Add(x1);
        geometry.Add(y1);
        geometry.Add(x2);
        geometry.Add(y2);
        geometry.Add(x);
        geometry.Add(y);
    }

    private static void AddQuadratic(List<byte> pathData, List<float> geometry, float controlX, float controlY, float x, float y)
    {
        pathData.Add((byte)PathOpcode.QuadTo);
        geometry.Add(controlX);
        geometry.Add(controlY);
        geometry.Add(x);
        geometry.Add(y);
    }

    private static void AddArc(
        List<byte> pathData,
        List<float> geometry,
        float startX,
        float startY,
        float radiusX,
        float radiusY,
        float rotation,
        bool largeArc,
        bool sweep,
        float endX,
        float endY)
    {
        if (startX == endX && startY == endY)
            return;
        if (radiusX == 0 || radiusY == 0)
        {
            AddLine(pathData, geometry, endX, endY);
            return;
        }

        pathData.Add((byte)PathOpcode.ArcTo);
        geometry.Add(radiusX);
        geometry.Add(radiusY);
        geometry.Add(rotation);
        geometry.Add(largeArc ? 1 : 0);
        geometry.Add(sweep ? 1 : 0);
        geometry.Add(endX);
        geometry.Add(endY);
    }

    private static (float X, float Y) ReadPoint(
        string[] tokens,
        ref int index,
        bool relative,
        float currentX,
        float currentY)
    {
        var x = ReadNumber(tokens, ref index);
        var y = ReadNumber(tokens, ref index);
        return relative ? (currentX + x, currentY + y) : (x, y);
    }

    private static float ReadNumber(string[] tokens, ref int index)
    {
        if (index >= tokens.Length || IsCommand(tokens[index]))
            throw new FormatException("SVG path command is missing a coordinate.");
        var value = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
        if (!float.IsFinite(value))
            throw new FormatException("SVG path coordinates must be finite.");
        return value;
    }

    private static bool ReadFlag(string[] tokens, ref int index)
    {
        var value = ReadNumber(tokens, ref index);
        if (value is not (0 or 1))
            throw new FormatException("SVG arc flags must be 0 or 1.");
        return value == 1;
    }

    private static bool IsCommand(string token) =>
        token.Length == 1 && char.IsLetter(token[0]);
}
