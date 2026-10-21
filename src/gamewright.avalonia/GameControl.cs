using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaColor = global::Avalonia.Media.Color;
using AvaloniaRect = global::Avalonia.Rect;

namespace Gamewright.Avalonia;

/// <summary>
/// Shows a <see cref="GameApp"/>: plays its static and dynamic frames onto an Avalonia drawing
/// context, letterboxed into the control, and turns pointer presses into <see cref="GameApp.Click"/>.
/// </summary>
public sealed class GameControl : Control
{
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
            var target = new DrawingTarget(context, app.SpriteSheet, spriteSheet);
            FramePlayer.Play(staticFrame, target);
            FramePlayer.Play(dynamicFrame, target);
        }
    }

    /// <summary>Turns the drawing operations of a played frame into Avalonia drawing calls.</summary>
    private sealed class DrawingTarget(DrawingContext context, SpriteSheetLayout? layout, IImage? sheet) : IFrameTarget
    {
        private StreamGeometry? geometry;
        private StreamGeometryContext? figures;
        private bool figureOpen;

        public void BeginPath()
        {
            geometry = new StreamGeometry();
            figures = geometry.Open();
            figureOpen = false;
        }

        public void MoveTo(float x, float y)
        {
            EndFigure(false);
            figures!.BeginFigure(new Point(x, y), true);
            figureOpen = true;
        }

        public void LineTo(float x, float y) => figures!.LineTo(new Point(x, y), true);

        public void QuadTo(float controlX, float controlY, float x, float y) =>
            figures!.QuadraticBezierTo(new Point(controlX, controlY), new Point(x, y), true);

        public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y) =>
            figures!.CubicBezierTo(new Point(control1X, control1Y), new Point(control2X, control2Y), new Point(x, y), true);

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, float x, float y) =>
            figures!.ArcTo(new Point(x, y), new Size(radiusX, radiusY), rotation, largeArc,
                sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise, true);

        public void ClosePath() => EndFigure(true);

        public void FillPath(uint rgba) =>
            context.DrawGeometry(new SolidColorBrush(ToColor(rgba)), null, Finished());

        public void StrokePath(uint rgba, float width) =>
            context.DrawGeometry(null, new Pen(new SolidColorBrush(ToColor(rgba)), width), Finished());

        public void DrawText(string text, float x, float y, TextStyle style, TextAlignment alignment)
        {
            var (typeface, size) = Font((FontIndex)style.Font);
            var formatted = new FormattedText(
                text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size,
                new SolidColorBrush(ToColor(style.ColorRgba)));

            var left = alignment switch
            {
                TextAlignment.Center => x - formatted.Width / 2,
                TextAlignment.End => x - formatted.Width,
                _ => x
            };

            // The text's vertical middle sits at y.
            context.DrawText(formatted, new Point(left, y - formatted.Height / 2));
        }

        public void DrawSprite(int spriteIndex, float x, float y, float width, float height)
        {
            if (sheet is null || layout is not { } cells)
                return;

            // The image may report its size in device independent units, so scale the cell to match.
            var cell = cells.CellSize * sheet.Size.Width / cells.Width;
            context.DrawImage(
                sheet,
                new AvaloniaRect(spriteIndex % cells.Columns * cell, spriteIndex / cells.Columns * cell, cell, cell),
                new AvaloniaRect(x, y, width, height));
        }

        private void EndFigure(bool close)
        {
            if (!figureOpen)
                return;
            figures!.EndFigure(close);
            figureOpen = false;
        }

        // Fill and stroke both draw the same finished geometry.
        private StreamGeometry Finished()
        {
            if (figures is not null)
            {
                EndFigure(false);
                figures.Dispose();
                figures = null;
            }
            return geometry!;
        }

        // The same fonts as the browser renderer: 16px body text and 24px semi-bold headings.
        private static (Typeface Typeface, double Size) Font(FontIndex font) => font switch
        {
            FontIndex.Body => (new Typeface(FontFamily.Default), 16),
            FontIndex.Heading => (new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 24),
            _ => throw new InvalidDataException($"Font index {(int)font} is out of range.")
        };

        private static AvaloniaColor ToColor(uint rgba) =>
            AvaloniaColor.FromArgb((byte)(rgba >> 24), (byte)rgba, (byte)(rgba >> 8), (byte)(rgba >> 16));
    }
}
