namespace Gamewright;

/// <summary>
/// Plays a <see cref="Frame"/> onto an <see cref="IFrameTarget"/>: decodes the commands, path
/// bytecode and style tables, checks them, and turns them into drawing operations.
/// </summary>
public static class FramePlayer
{
    /// <exception cref="InvalidDataException">The frame is malformed.</exception>
    public static void Play<TTarget>(Frame frame, TTarget target)
        where TTarget : IFrameTarget
    {
        ArgumentNullException.ThrowIfNull(frame);

        foreach (var command in frame.Commands)
        {
            switch (command.Type)
            {
                case DrawCommandType.Path:
                    PlayPath(frame, command, target);
                    break;
                case DrawCommandType.Text:
                    PlayText(frame, command, target);
                    break;
                case DrawCommandType.Image:
                    PlaySprite(frame, command, target);
                    break;
                default:
                    throw Invalid($"Unknown draw command type: {command.Type}.");
            }
        }
    }

    private static void PlayPath<TTarget>(Frame frame, DrawCommand command, TTarget target)
        where TTarget : IFrameTarget
    {
        if (command.StyleIndex >= frame.Paints.Length)
            throw Invalid($"Paint index {command.StyleIndex} is out of range.");
        var paint = frame.Paints[command.StyleIndex];
        if (paint.IsEmpty)
            throw Invalid("A path needs a paint with a fill or a stroke.");

        var pathOffset = command.PathDataOffset;
        var pathEnd = pathOffset + command.PathDataLength;
        var geometryOffset = command.GeometryOffset;
        var geometryEnd = geometryOffset + command.GeometryLength;
        if (pathOffset < 0 || command.PathDataLength < 0 || pathEnd > frame.PathData.Length
            || geometryOffset < 0 || command.GeometryLength < 0 || geometryEnd > frame.Geometry.Length)
        {
            throw Invalid("Path geometry range is out of bounds.");
        }

        float Next()
        {
            if (geometryOffset >= geometryEnd)
                throw Invalid("A path operation is missing coordinates.");
            return frame.Geometry[geometryOffset++];
        }

        target.BeginPath();

        var figureOpen = false;
        float currentX = 0, currentY = 0, startX = 0, startY = 0;

        // A subpath continues from the start of the one just closed, so give it an explicit MoveTo.
        void EnsureFigure()
        {
            if (figureOpen)
                return;
            if (pathOffset - 1 == command.PathDataOffset)
                throw Invalid("A path must begin with MoveTo.");
            target.MoveTo(startX, startY);
            currentX = startX;
            currentY = startY;
            figureOpen = true;
        }

        while (pathOffset < pathEnd)
        {
            var opcode = (PathOpcode)frame.PathData[pathOffset++];
            switch (opcode)
            {
                case PathOpcode.MoveTo:
                    currentX = startX = Next();
                    currentY = startY = Next();
                    target.MoveTo(currentX, currentY);
                    figureOpen = true;
                    break;
                case PathOpcode.LineTo:
                    {
                        var x = Next();
                        var y = Next();
                        EnsureFigure();
                        target.LineTo(x, y);
                        currentX = x;
                        currentY = y;
                        break;
                    }
                case PathOpcode.QuadTo:
                    {
                        var controlX = Next();
                        var controlY = Next();
                        var x = Next();
                        var y = Next();
                        EnsureFigure();
                        target.QuadTo(controlX, controlY, x, y);
                        currentX = x;
                        currentY = y;
                        break;
                    }
                case PathOpcode.CubicTo:
                    {
                        var control1X = Next();
                        var control1Y = Next();
                        var control2X = Next();
                        var control2Y = Next();
                        var x = Next();
                        var y = Next();
                        EnsureFigure();
                        target.CubicTo(control1X, control1Y, control2X, control2Y, x, y);
                        currentX = x;
                        currentY = y;
                        break;
                    }
                case PathOpcode.ArcTo:
                    {
                        var radiusX = Math.Abs(Next());
                        var radiusY = Math.Abs(Next());
                        var rotation = Next();
                        var largeArc = Next() == 1;
                        var sweep = Next() == 1;
                        var x = Next();
                        var y = Next();
                        EnsureFigure();
                        if (x == currentX && y == currentY)
                            break;
                        if (radiusX == 0 || radiusY == 0)
                            target.LineTo(x, y);
                        else
                            target.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, x, y);
                        currentX = x;
                        currentY = y;
                        break;
                    }
                case PathOpcode.ClosePath:
                    if (figureOpen)
                    {
                        target.ClosePath();
                        figureOpen = false;
                    }
                    currentX = startX;
                    currentY = startY;
                    break;
                default:
                    throw Invalid($"Unknown path operation: {opcode}.");
            }
        }

        if (geometryOffset != geometryEnd)
            throw Invalid("Path geometry length does not match its operations.");

        if (figureOpen && (command.Flags & (ushort)DrawCommandFlags.Closed) != 0)
            target.ClosePath();

        switch (paint.Mode)
        {
            case PaintMode.Fill:
                target.FillPath(paint.FillRgba);
                break;
            case PaintMode.Stroke:
                target.StrokePath(paint.StrokeRgba, paint.StrokeWidth);
                break;
            case PaintMode.FillThenStroke:
                target.FillPath(paint.FillRgba);
                target.StrokePath(paint.StrokeRgba, paint.StrokeWidth);
                break;
            case PaintMode.StrokeThenFill:
                target.StrokePath(paint.StrokeRgba, paint.StrokeWidth);
                target.FillPath(paint.FillRgba);
                break;
            default:
                throw Invalid($"Unknown paint mode: {paint.Mode}.");
        }
    }

    private static void PlayText<TTarget>(Frame frame, DrawCommand command, TTarget target)
        where TTarget : IFrameTarget
    {
        if (command.StyleIndex >= frame.TextStyles.Length)
            throw Invalid($"Text style index {command.StyleIndex} is out of range.");
        if (command.Data < 0 || command.Data >= frame.Strings.Length)
            throw Invalid($"String index {command.Data} is out of range.");
        RequireGeometry(frame, command, 2);

        var alignment = (command.Flags & (ushort)DrawCommandFlags.AlignCenter) != 0
            ? TextAlignment.Center
            : (command.Flags & (ushort)DrawCommandFlags.AlignEnd) != 0 ? TextAlignment.End : TextAlignment.Start;
        target.DrawText(
            frame.Strings[command.Data],
            frame.Geometry[command.GeometryOffset],
            frame.Geometry[command.GeometryOffset + 1],
            frame.TextStyles[command.StyleIndex],
            alignment);
    }

    private static void PlaySprite<TTarget>(Frame frame, DrawCommand command, TTarget target)
        where TTarget : IFrameTarget
    {
        RequireGeometry(frame, command, 4);

        var geometry = command.GeometryOffset;
        target.DrawSprite(
            command.Data,
            frame.Geometry[geometry],
            frame.Geometry[geometry + 1],
            frame.Geometry[geometry + 2],
            frame.Geometry[geometry + 3]);
    }

    private static void RequireGeometry(Frame frame, DrawCommand command, int count)
    {
        if (command.GeometryOffset < 0 || command.GeometryLength < count
            || command.GeometryOffset + count > frame.Geometry.Length)
        {
            throw Invalid($"Draw command {command.Type} has incomplete geometry.");
        }
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
