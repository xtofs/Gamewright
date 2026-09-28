namespace Gamewright.Graphics.Geometry;

using Gamewright.Graphics;


using System.Numerics;
using System.Runtime.InteropServices;

internal static class GeometryBuilder
{
    public static RoundedBoxInstance CreateRoundedBox(
        Rect rectangle,
        CornerRadii radii,
        Color color,
        Stroke stroke)
    {
        return new RoundedBoxInstance
        {
            Center = rectangle.Position + rectangle.Size * 0.5f,
            HalfExtent = rectangle.Size * 0.5f,
            CornerRadii = new Vector4(radii.BottomRight, radii.TopRight, radii.TopLeft, radii.BottomLeft),
            FillColor = color.Vector4,
            StrokeColor = stroke.Color.Vector4,
            StrokeWidth = stroke.Width,
        };
    }


    public static RoundedBoxInstance CreateCircle(Vector2 center, float radius, Color color)
    {
        return CreateRoundedBox(
            new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2),
            new CornerRadii(radius),
            color,
            default);
    }

    public static CapsuleInstance CreateCapsule(
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        Stroke stroke)
    {
        return new CapsuleInstance
        {
            Start = start,
            End = end,
            Thickness = thickness,
            FillColor = color.Vector4,
            StrokeColor = stroke.Color.Vector4,
            StrokeWidth = stroke.Width,
        };
    }

    public static IReadOnlyList<ColoredTriangleInstance> CreatePolygonFill(ReadOnlySpan<Vector2> points, Color color)
    {
        var triangles = new List<ColoredTriangleInstance>(points.Length - 2);
        var colorVec = color.Vector4;
        for (var i = 1; i < points.Length - 1; i++)
        {
            triangles.Add(new ColoredTriangleInstance { V0 = points[0], V1 = points[i], V2 = points[i + 1], Color = colorVec });
        }

        return triangles;
    }

    public static PolylineInstances CreatePolyline(ReadOnlySpan<Vector2> points, float thickness, Color color)
    {
        var segments = new List<CapsuleInstance>(Math.Max(0, points.Length - 1));
        var joins = new List<RoundedBoxInstance>(Math.Max(0, points.Length - 2));
        for (var index = 1; index < points.Length; index++)
        {
            if (points[index - 1] != points[index])
            {
                segments.Add(CreateCapsule(points[index - 1], points[index], thickness, color, default));
            }
        }

        for (var index = 1; index < points.Length - 1; index++)
        {
            if (points[index - 1] != points[index] && points[index] != points[index + 1])
            {
                joins.Add(CreateCircle(points[index], thickness * 0.5f, color));
            }
        }

        return new PolylineInstances(segments, joins);
    }

    public static QuadraticBezierInstance CreateQuadraticBezier(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float thickness,
        Color color)
    {
        // a collinear control point only changes the speed along a straight line;
        // moving it to the middle makes the shader draw the piece as a plain segment
        var chord = end - start;
        var cross = chord.X * (control.Y - start.Y) - chord.Y * (control.X - start.X);
        if (MathF.Abs(cross) <= 1e-4f * MathF.Max(chord.LengthSquared(), (control - start).LengthSquared()))
        {
            control = (start + end) * 0.5f;
        }

        return new QuadraticBezierInstance
        {
            Start = start,
            Control = control,
            End = end,
            Thickness = thickness,
            Color = color.Vector4,
        };
    }

    /// <summary>
    /// Splits the uniform quadratic B-spline of <paramref name="points"/>, with doubled end points,
    /// into Bézier pieces: a line from the first point to the first midpoint, one curve per interior point
    /// (from the previous midpoint, controlled by the point, to the next midpoint), and a line from the
    /// last midpoint to the last point.
    /// </summary>
    public static IReadOnlyList<QuadraticBezierInstance> CreateSpline(ReadOnlySpan<Vector2> points, float thickness, Color color)
    {
        var pieces = new List<QuadraticBezierInstance>(Math.Max(0, points.Length));
        if (points.Length < 2)
        {
            return pieces;
        }

        // the control polygon with the first and last point doubled
        var control = new Vector2[points.Length + 2];
        control[0] = points[0];
        points.CopyTo(control.AsSpan(1));
        control[^1] = points[^1];

        for (var index = 1; index < control.Length - 1; index++)
        {
            var start = (control[index - 1] + control[index]) * 0.5f;
            var end = (control[index] + control[index + 1]) * 0.5f;
            if (start != end)
            {
                pieces.Add(CreateQuadraticBezier(start, control[index], end, thickness, color));
            }
        }

        return pieces;
    }
}

internal sealed record PolylineInstances(
    IReadOnlyList<CapsuleInstance> Segments,
    IReadOnlyList<RoundedBoxInstance> Joins);

[StructLayout(LayoutKind.Sequential)]
internal struct RoundedBoxInstance
{
    public Vector2 Center;
    public Vector2 HalfExtent;
    public Vector4 CornerRadii;
    public Vector4 FillColor;
    public Vector4 StrokeColor;
    public float StrokeWidth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CapsuleInstance
{
    public Vector2 Start;
    public Vector2 End;
    public float Thickness;
    public Vector4 FillColor;
    public Vector4 StrokeColor;
    public float StrokeWidth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct QuadraticBezierInstance
{
    public Vector2 Start;
    public Vector2 Control;
    public Vector2 End;
    public float Thickness;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TexturedQuadInstance
{
    public Vector4 Destination;
    public Vector4 UvRectangle;
    public Vector4 Tint;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ArrowheadInstance
{
    public Vector2 Tip;
    public Vector2 Base;
    public float HalfWidth;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ColoredTriangleInstance
{
    public Vector2 V0;
    public Vector2 V1;
    public Vector2 V2;
    public Vector4 Color;
}
