namespace Gamewright;

using System.Globalization;
using System.Text.RegularExpressions;

internal static class SvgPathParser
{
    private static readonly Regex TokenPattern = new(
        @"[a-zA-Z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?",
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

    // An arc flag is a single '0' or '1' character, which SVG allows to be written without a
    // separator ("a5 5 0 01.5 1.5"), so it may be the first character of a longer token.
    private static bool ReadFlag(string[] tokens, ref int index)
    {
        if (index >= tokens.Length || IsCommand(tokens[index]))
            throw new FormatException("SVG path command is missing a coordinate.");

        var token = tokens[index];
        if (token[0] is not ('0' or '1'))
            throw new FormatException("SVG arc flags must be 0 or 1.");

        if (token.Length == 1)
            index++;
        else
            tokens[index] = token[1..];
        return token[0] == '1';
    }

    private static bool IsCommand(string token) =>
        token.Length == 1 && char.IsLetter(token[0]);
}
