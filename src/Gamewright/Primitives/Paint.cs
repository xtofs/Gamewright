namespace Gamewright;

using System.Runtime.InteropServices;

/// <summary>How a path is painted (<c>docs/frame-spec.md</c> section 5). The order matters: a stroke is centred on the edge, so stroking after the fill shows its full width, while stroking first leaves only the outer half visible.</summary>
public enum PaintMode : uint
{
    /// <summary>Nothing: the value of <c>default(Paint)</c>, which cannot be drawn.</summary>
    None = 0,
    Fill = 1,
    Stroke = 2,
    FillThenStroke = 3,
    StrokeThenFill = 4
}

/// <summary>
/// How a path is filled and/or stroked. Built from <see cref="Fill(Color)"/> or
/// <see cref="Stroke(Color, float)"/> and optionally extended with the other part; the order of
/// the calls is the paint order, so <c>Paint.Fill(a).WithStroke(b, 2)</c> strokes over the fill
/// and <c>Paint.Stroke(b, 2).WithFill(a)</c> fills over the stroke. Sent to the renderer as 16
/// bytes: fill, stroke, stroke width and mode.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Paint
{
    private readonly uint fillRgba;
    private readonly uint strokeRgba;
    private readonly float strokeWidth;
    private readonly PaintMode mode;

    private Paint(uint fillRgba, uint strokeRgba, float strokeWidth, PaintMode mode)
    {
        this.fillRgba = fillRgba;
        this.strokeRgba = strokeRgba;
        this.strokeWidth = strokeWidth;
        this.mode = mode;
    }

    public PaintMode Mode => mode;

    public uint FillRgba => fillRgba;

    public uint StrokeRgba => strokeRgba;

    public float StrokeWidth => strokeWidth;

    public bool IsEmpty => mode == PaintMode.None;

    public bool HasFill => mode is PaintMode.Fill or PaintMode.FillThenStroke or PaintMode.StrokeThenFill;

    public bool HasStroke => mode is PaintMode.Stroke or PaintMode.FillThenStroke or PaintMode.StrokeThenFill;

    public static Paint Fill(Color color) => new(color.Rgba, 0, 0, PaintMode.Fill);

    public static Paint Stroke(Color color, float width)
    {
        RequireValidWidth(width);
        return new Paint(0, color.Rgba, width, PaintMode.Stroke);
    }

    /// <summary>Adds or replaces the fill. A stroke-only paint becomes stroke-then-fill; a paint with both keeps its order.</summary>
    public Paint WithFill(Color color) => new(
        color.Rgba,
        strokeRgba,
        strokeWidth,
        mode switch
        {
            PaintMode.None or PaintMode.Fill => PaintMode.Fill,
            PaintMode.Stroke => PaintMode.StrokeThenFill,
            _ => mode
        });

    /// <summary>Adds or replaces the stroke. A fill-only paint becomes fill-then-stroke; a paint with both keeps its order.</summary>
    public Paint WithStroke(Color color, float width)
    {
        RequireValidWidth(width);
        return new Paint(
            fillRgba,
            color.Rgba,
            width,
            mode switch
            {
                PaintMode.None or PaintMode.Stroke => PaintMode.Stroke,
                PaintMode.Fill => PaintMode.FillThenStroke,
                _ => mode
            });
    }

    public static implicit operator Paint(Color color) => Fill(color);

    public static implicit operator Paint(StrokeStyle stroke) => Stroke(stroke.Color, stroke.Width);

    private static void RequireValidWidth(float width)
    {
        if (!(width > 0) || !float.IsFinite(width))
            throw new ArgumentOutOfRangeException(nameof(width), "Stroke width must be finite and positive.");
    }
}
