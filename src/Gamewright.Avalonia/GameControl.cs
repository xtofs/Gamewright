using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaColor = global::Avalonia.Media.Color;
using AvaloniaRect = global::Avalonia.Rect;

namespace Gamewright.Avalonia;

/// <summary>
/// Shows a <see cref="GameApp"/>: draws its static and dynamic frames, letterboxed into the control,
/// and turns pointer presses into <see cref="GameApp.Click"/>. The frames are interpreted here
/// directly into Avalonia drawing calls.
/// </summary>
public sealed class GameControl : Control
{
    private const double FrameEpsilon = 1e-6;

    private readonly GameApp app;
    private readonly IImage? spriteSheet;
    private Frame staticFrame;
    private Frame dynamicFrame;

    /// <param name="app">The app to show.</param>
    /// <param name="spriteSheet">The image named by the app's <see cref="GameApp.SpriteSheet"/>, if it has one.</param>
    public GameControl(GameApp app, IImage? spriteSheet = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        this.app = app;
        this.spriteSheet = spriteSheet;
        staticFrame = app.RenderStatic();
        dynamicFrame = app.RenderDynamic();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var box = CurrentLetterbox();
        if (box.IsEmpty)
            return;

        var position = e.GetPosition(this);
        var (x, y) = box.ToFrame((float)position.X, (float)position.Y);
        Click(x, y);
    }

    /// <summary>Clicks at a position in frame coordinates, as a pointer press there would.</summary>
    public void Click(float x, float y) => Apply(app.Click(x, y));

    private void Apply(Invalidation invalidation)
    {
        if (invalidation.HasFlag(Invalidation.Static))
            staticFrame = app.RenderStatic();
        if (invalidation.HasFlag(Invalidation.Dynamic))
            dynamicFrame = app.RenderDynamic();
        if (invalidation != Invalidation.None)
            InvalidateVisual();
    }

    private Letterbox CurrentLetterbox() =>
        Letterbox.Fit(app.FrameWidth, app.FrameHeight, (float)Bounds.Width, (float)Bounds.Height);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var box = CurrentLetterbox();
        if (box.IsEmpty)
            return;

        var transform = Matrix.CreateScale(box.Scale, box.Scale) * Matrix.CreateTranslation(box.OffsetX, box.OffsetY);
        using (context.PushTransform(transform))
        {
            Draw(context, staticFrame);
            Draw(context, dynamicFrame);
        }
    }

    private void Draw(DrawingContext context, Frame frame)
    {
        foreach (var command in frame.Commands)
        {
            switch (command.Type)
            {
                case DrawCommandType.Path:
                    DrawPath(context, frame, command);
                    break;
                case DrawCommandType.Text:
                    DrawText(context, frame, command);
                    break;
                case DrawCommandType.Image:
                    DrawSprite(context, frame, command);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown draw command type: {command.Type}");
            }
        }
    }

    private static void DrawPath(DrawingContext context, Frame frame, DrawCommand command)
    {
        if (command.StyleIndex >= frame.Paints.Length)
            throw new InvalidOperationException($"Frame paint index {command.StyleIndex} is out of range.");
        var paint = frame.Paints[command.StyleIndex];

        var geometry = BuildGeometry(frame, command);
        var fill = new SolidColorBrush(ToColor(paint.FillRgba));
        var stroke = new Pen(new SolidColorBrush(ToColor(paint.StrokeRgba)), paint.StrokeWidth);

        switch (paint.Mode)
        {
            case PaintMode.Fill:
                context.DrawGeometry(fill, null, geometry);
                break;
            case PaintMode.Stroke:
                context.DrawGeometry(null, stroke, geometry);
                break;
            case PaintMode.FillThenStroke:
                context.DrawGeometry(fill, stroke, geometry);
                break;
            case PaintMode.StrokeThenFill:
                context.DrawGeometry(null, stroke, geometry);
                context.DrawGeometry(fill, null, geometry);
                break;
            default:
                throw new InvalidOperationException($"Unknown paint mode: {paint.Mode}");
        }
    }

    private static StreamGeometry BuildGeometry(Frame frame, DrawCommand command)
    {
        var pathOffset = command.PathDataOffset;
        var geometryOffset = command.GeometryOffset;
        var pathEnd = pathOffset + command.PathDataLength;
        var geometryEnd = geometryOffset + command.GeometryLength;
        if (pathOffset < 0 || pathEnd > frame.PathData.Length || geometryOffset < 0 || geometryEnd > frame.Geometry.Length)
            throw new InvalidOperationException("Path geometry range is out of bounds.");

        float Next()
        {
            if (geometryOffset >= geometryEnd)
                throw new InvalidOperationException("Path bytecode operation has missing coordinates.");
            return frame.Geometry[geometryOffset++];
        }

        Point NextPoint() => new(Next(), Next());

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.SetFillRule(FillRule.NonZero); // docs/frame-spec.md section 5
            var figureOpen = false;
            var current = default(Point);
            var subpathStart = default(Point);
            var lastWasClose = false;

            // A drawing operation after a close continues from the subpath start, as in an HTML canvas.
            void EnsureFigure()
            {
                if (figureOpen)
                    return;
                g.BeginFigure(current, true);
                figureOpen = true;
            }

            while (pathOffset < pathEnd)
            {
                var opcode = (PathOpcode)frame.PathData[pathOffset++];
                lastWasClose = opcode == PathOpcode.ClosePath;
                switch (opcode)
                {
                    case PathOpcode.MoveTo:
                        if (figureOpen)
                            g.EndFigure(false);
                        current = subpathStart = NextPoint();
                        g.BeginFigure(current, true);
                        figureOpen = true;
                        break;
                    case PathOpcode.LineTo:
                        {
                            var end = NextPoint();
                            EnsureFigure();
                            g.LineTo(end, true);
                            current = end;
                            break;
                        }
                    case PathOpcode.QuadTo:
                        {
                            var control = NextPoint();
                            var end = NextPoint();
                            EnsureFigure();
                            g.QuadraticBezierTo(control, end, true);
                            current = end;
                            break;
                        }
                    case PathOpcode.CubicTo:
                        {
                            var control1 = NextPoint();
                            var control2 = NextPoint();
                            var end = NextPoint();
                            EnsureFigure();
                            g.CubicBezierTo(control1, control2, end, true);
                            current = end;
                            break;
                        }
                    case PathOpcode.ArcTo:
                        {
                            var radiusX = Math.Abs(Next());
                            var radiusY = Math.Abs(Next());
                            var rotation = Next();
                            var largeArc = Next() == 1;
                            var sweep = Next() == 1;
                            var end = NextPoint();
                            EnsureFigure();
                            if (Math.Abs(end.X - current.X) < FrameEpsilon && Math.Abs(end.Y - current.Y) < FrameEpsilon)
                                break;
                            if (radiusX == 0 || radiusY == 0)
                                g.LineTo(end, true);
                            else
                                g.ArcTo(end, new Size(radiusX, radiusY), rotation, largeArc,
                                    sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise, true);
                            current = end;
                            break;
                        }
                    case PathOpcode.ClosePath:
                        if (figureOpen)
                        {
                            g.EndFigure(true);
                            figureOpen = false;
                        }
                        current = subpathStart;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown path bytecode operation: {opcode}");
                }
            }

            if (geometryOffset != geometryEnd)
                throw new InvalidOperationException("Path geometry length does not match its bytecode.");

            if (figureOpen)
                g.EndFigure(HasFlag(command, DrawCommandFlags.Closed) && !lastWasClose);
        }
        return geometry;
    }

    private void DrawText(DrawingContext context, Frame frame, DrawCommand command)
    {
        if (command.StyleIndex >= frame.TextStyles.Length)
            throw new InvalidOperationException($"Frame text style index {command.StyleIndex} is out of range.");
        if (command.Data < 0 || command.Data >= frame.Strings.Length)
            throw new InvalidOperationException($"Frame string index {command.Data} is out of range.");

        var style = frame.TextStyles[command.StyleIndex];
        var (typeface, size) = Font((FontIndex)style.Font);
        var text = new FormattedText(
            frame.Strings[command.Data],
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            new SolidColorBrush(ToColor(style.ColorRgba)));

        var x = frame.Geometry[command.GeometryOffset];
        var y = frame.Geometry[command.GeometryOffset + 1];
        if (HasFlag(command, DrawCommandFlags.AlignCenter))
            x -= (float)text.Width / 2;
        else if (HasFlag(command, DrawCommandFlags.AlignEnd))
            x -= (float)text.Width;

        // y is the middle of the em square: the baseline lies (ascent - descent) / 2 below it
        // (docs/frame-spec.md section 3). FormattedText.Baseline is the baseline's distance from the top.
        var metrics = typeface.GlyphTypeface.Metrics;
        var emScale = size / metrics.DesignEmHeight;
        var baselineBelowY = (Math.Abs(metrics.Ascent) - Math.Abs(metrics.Descent)) * emScale / 2;
        context.DrawText(text, new Point(x, y + baselineBelowY - text.Baseline));
    }

    private void DrawSprite(DrawingContext context, Frame frame, DrawCommand command)
    {
        if (spriteSheet is null || app.SpriteSheet is not { } layout)
            return;

        var x = frame.Geometry[command.GeometryOffset];
        var y = frame.Geometry[command.GeometryOffset + 1];
        var width = frame.Geometry[command.GeometryOffset + 2];
        var height = frame.Geometry[command.GeometryOffset + 3];

        // The image may report its size in device independent units, so scale the cell to match.
        var imageScale = spriteSheet.Size.Width / layout.Width;
        var cell = layout.CellSize * imageScale;
        var column = command.Data % layout.Columns;
        var row = command.Data / layout.Columns;
        context.DrawImage(spriteSheet, new AvaloniaRect(column * cell, row * cell, cell, cell), new AvaloniaRect(x, y, width, height));
    }

    // The same fonts as the browser renderer: 16px body text and 24px semi-bold headings.
    private static (Typeface Typeface, double Size) Font(FontIndex font) => font switch
    {
        FontIndex.Body => (new Typeface(FontFamily.Default), 16),
        FontIndex.Heading => (new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 24),
        _ => throw new InvalidOperationException($"Font index {(int)font} is out of range.")
    };

    private static bool HasFlag(DrawCommand command, DrawCommandFlags flag) => (command.Flags & (ushort)flag) != 0;

    private static AvaloniaColor ToColor(uint rgba) =>
        AvaloniaColor.FromArgb((byte)(rgba >> 24), (byte)rgba, (byte)(rgba >> 8), (byte)(rgba >> 16));
}
