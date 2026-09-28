namespace Gamewright.Graphics;


using System.Numerics;
using System.Runtime.InteropServices;
using Gamewright.Graphics.Generated;
using Gamewright.Graphics.Geometry;
using Gamewright.Graphics.Rendering;
using Silk.NET.OpenGL;

public sealed class Canvas : IDisposable
{
    private readonly GL _gl;
    private readonly BuiltInShaderPrograms _programs;
    private readonly FrameUniformBuffer _frameBuffer;
    private readonly InstanceBatch<RoundedBoxInstance> _roundedBoxes;
    private readonly InstanceBatch<CapsuleInstance> _capsules;
    private readonly InstanceBatch<QuadraticBezierInstance> _beziers;
    private readonly InstanceBatch<TexturedQuadInstance> _texturedQuads;
    private readonly InstanceBatch<ArrowheadInstance> _arrowheads;
    private readonly InstanceBatch<ColoredTriangleInstance> _triangles;
    private readonly IViewport _viewport;

    private float _time;

    private BatchKind _activeBatch;
    private Texture2D? _activeTexture;
    private bool _begun;
    private bool _disposed;

    public Canvas(GL gl, IViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(viewport);
        _viewport = viewport;
        _gl = gl;
        _programs = new BuiltInShaderPrograms(gl);
        _frameBuffer = new FrameUniformBuffer(gl, RoundedBoxShaderBindings.FrameBinding);
        _roundedBoxes = new InstanceBatch<RoundedBoxInstance>(gl, RoundedBoxAttributes);
        _capsules = new InstanceBatch<CapsuleInstance>(gl, CapsuleAttributes);
        _beziers = new InstanceBatch<QuadraticBezierInstance>(gl, QuadraticBezierAttributes);
        _texturedQuads = new InstanceBatch<TexturedQuadInstance>(gl, TexturedQuadAttributes);
        _arrowheads = new InstanceBatch<ArrowheadInstance>(gl, ArrowAttributes);
        _triangles = new InstanceBatch<ColoredTriangleInstance>(gl, TriangleAttributes);
    }

    internal GL GL => _gl;
    public Vector2 FramebufferSize => _viewport.FramebufferSize;

    public void Begin(float delta)
    {
        _time += delta;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_begun)
        {
            throw new InvalidOperationException("End must be called before beginning another frame.");
        }

        var viewportSize = FramebufferSize;

        var projection = Matrix4x4.CreateOrthographicOffCenter(0, viewportSize.X, viewportSize.Y, 0, -1, 1);
        _frameBuffer.Update(projection, viewportSize, _time);
        _gl.Viewport(0, 0, (uint)viewportSize.X, (uint)viewportSize.Y);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _activeBatch = BatchKind.None;
        _activeTexture = null;
        _begun = true;
    }
    public void Clear(Color color)
    {
        _gl.ClearColor(color.R, color.G, color.B, color.A);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void DrawRectangle(Rect rectangle, Color? color = null, Stroke? stroke = null)
    {
        DrawRoundedRectangle(rectangle, default, color, stroke);
    }

    public void DrawRoundedRectangle(Rect rectangle, CornerRadii radii, Color? color = null, Stroke? stroke = null)
    {
        EnsureDrawing();
        ValidateRectangle(rectangle);
        var actualStroke = stroke ?? default;
        if (actualStroke.Width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), "Stroke width cannot be negative.");
        }
        var actualColor = color ??= default!;

        SwitchBatch(BatchKind.RoundedBox, null);
        _roundedBoxes.Add(GeometryBuilder.CreateRoundedBox(rectangle, radii, actualColor, actualStroke));
    }

    public void DrawCircle(Vector2 center, float radius, Color color, Stroke? stroke = null)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");
        }

        DrawRoundedRectangle(
            new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2),
            new CornerRadii(radius),
            color,
            stroke);
    }

    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color, Stroke? stroke = null)
    {
        EnsureDrawing();
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        var actualStroke = stroke ?? default;
        if (actualStroke.Width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), "Stroke width cannot be negative.");
        }

        SwitchBatch(BatchKind.Capsule, null);
        _capsules.Add(GeometryBuilder.CreateCapsule(start, end, thickness, color, actualStroke));
    }

    public void DrawPolyline(ReadOnlySpan<Vector2> points, float thickness, Color color)
    {
        EnsureDrawing();
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        var instances = GeometryBuilder.CreatePolyline(points, thickness, color);
        if (instances.Segments.Count > 0)
        {
            SwitchBatch(BatchKind.Capsule, null);
            foreach (var segment in instances.Segments)
            {
                _capsules.Add(segment);
            }
        }

        if (instances.Joins.Count > 0)
        {
            SwitchBatch(BatchKind.RoundedBox, null);
            foreach (var join in instances.Joins)
            {
                _roundedBoxes.Add(join);
            }
        }
    }

    /// <summary>Draws the quadratic Bézier curve from <paramref name="start"/> to <paramref name="end"/>, pulled towards <paramref name="control"/>.</summary>
    public void DrawQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float thickness, Color color)
    {
        EnsureDrawing();
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        SwitchBatch(BatchKind.QuadraticBezier, null);
        _beziers.Add(GeometryBuilder.CreateQuadraticBezier(start, control, end, thickness, color));
    }

    /// <summary>
    /// Draws a smooth curve along the polygon through <paramref name="points"/>. The curve passes through
    /// the first point, the midpoint of every segment of the polygon, and the last point, and is tangent
    /// to the polygon at each midpoint. Where the polygon runs straight, so does the curve.
    /// </summary>
    public void DrawSpline(ReadOnlySpan<Vector2> points, float thickness, Color color)
    {
        EnsureDrawing();
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        var pieces = GeometryBuilder.CreateSpline(points, thickness, color);
        if (pieces.Count > 0)
        {
            SwitchBatch(BatchKind.QuadraticBezier, null);
            foreach (var piece in pieces)
            {
                _beziers.Add(piece);
            }
        }
    }

    public void DrawArrow(
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color,
        ArrowheadStyle tips = ArrowheadStyle.End)
    {
        EnsureDrawing();
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        var headLength = thickness * 2.5f;
        var halfHeadWidth = thickness * 1.5f;

        var delta = to - from;
        var length = delta.Length();
        if (length < 1e-6f)
        {
            return;
        }

        var dir = delta / length;

        var shaftFrom = from;
        var shaftTo = to;

        if ((tips & ArrowheadStyle.End) != 0)
        {
            shaftTo = to - dir * MathF.Min(headLength, length * 0.5f);
            SwitchBatch(BatchKind.Arrow, null);
            _arrowheads.Add(new ArrowheadInstance { Tip = to, Base = shaftTo, HalfWidth = halfHeadWidth, Color = color.Vector4 });
        }

        if ((tips & ArrowheadStyle.Start) != 0)
        {
            shaftFrom = from + dir * MathF.Min(headLength, length * 0.5f);
            SwitchBatch(BatchKind.Arrow, null);
            _arrowheads.Add(new ArrowheadInstance { Tip = from, Base = shaftFrom, HalfWidth = halfHeadWidth, Color = color.Vector4 });
        }

        if (shaftFrom != shaftTo)
        {
            SwitchBatch(BatchKind.Capsule, null);
            _capsules.Add(GeometryBuilder.CreateCapsule(shaftFrom, shaftTo, thickness, color, default));
        }
    }

    public void DrawPolygon(ReadOnlySpan<Vector2> points, Color? fill = null, Stroke? stroke = null)
    {
        EnsureDrawing();
        if (points.Length < 3)
        {
            throw new ArgumentException("A polygon requires at least 3 points.", nameof(points));
        }

        if (stroke is { Width: < 0 })
        {
            throw new ArgumentOutOfRangeException(nameof(stroke), "Stroke width cannot be negative.");
        }

        if (fill is { } fillColor)
        {
            var triangles = GeometryBuilder.CreatePolygonFill(points, fillColor);
            SwitchBatch(BatchKind.Triangle, null);
            foreach (var tri in triangles)
            {
                _triangles.Add(tri);
            }
        }

        if (stroke is { Width: > 0 } s)
        {
            SwitchBatch(BatchKind.Capsule, null);
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                _capsules.Add(GeometryBuilder.CreateCapsule(a, b, s.Width, s.Color, default));
            }
        }
    }

    public void DrawSprite(
        Texture2D texture,
        Rect destination,
        TextureRegion? source = null,
        Color? tint = null)
    {
        DrawTexturedQuad(BatchKind.Sprite, texture, destination, source ?? TextureRegion.Full, tint ?? Colors.White);
    }

    public void DrawSprite<TKey>(
        TextureAtlas<TKey> atlas,
        TKey key,
        Rect destination,
        Color? tint = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(atlas);
        DrawTexturedQuad(BatchKind.Sprite, atlas.Texture, destination, atlas[key], tint ?? Colors.White);
    }

    public void DrawGlyph(Texture2D atlas, Rect destination, TextureRegion source, Color tint)
    {
        DrawTexturedQuad(BatchKind.Glyph, atlas, destination, source, tint);
    }

    public void DrawText(
        Font atlas,
        string text,
        Vector2 position,
        Color tint,
        float scale = 1)
    {
        EnsureDrawing();
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(text);

        foreach (var glyph in atlas.LayoutAtOrigin(text, scale))
        {
            DrawTexturedQuad(BatchKind.Glyph, atlas.Texture, glyph.Destination + position, glyph.Region, tint);
        }
    }

    /// <summary>Returns the largest scale at which <paramref name="text"/> fits inside <paramref name="rect"/>.</summary>
    public float MeasureFitScale(Font font, string text, Rect rect)
    {
        var naturalSize = font.Measure(text, scale: 1f);
        return MathF.Min(rect.Width / naturalSize.X, rect.Height / naturalSize.Y);
    }

    /// <summary>Draws <paramref name="text"/> centered in <paramref name="rect"/> at an automatically computed scale.</summary>
    public void DrawCenteredText(Font font, string text, Rect rect, Color? tint = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        DrawCenteredText(font, text, rect, MeasureFitScale(font, text, rect), tint);
    }

    /// <summary>Draws <paramref name="text"/> centered in <paramref name="rect"/> at the given <paramref name="scale"/>.</summary>
    public void DrawCenteredText(Font font, string text, Rect rect, float scale, Color? tint = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var textSize = font.Measure(text, scale);
        var origin = rect.Position + (rect.Size - textSize) / 2f;
        DrawText(font, text, origin, tint ?? Colors.Black, scale);
    }

    public void End()
    {
        EnsureDrawing();
        Flush();
        _begun = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _triangles.Dispose();
        _arrowheads.Dispose();
        _texturedQuads.Dispose();
        _beziers.Dispose();
        _capsules.Dispose();
        _roundedBoxes.Dispose();
        _frameBuffer.Dispose();
        _programs.Dispose();
        _disposed = true;
    }

    private static IReadOnlyList<InstanceAttribute> RoundedBoxAttributes { get; } =
    [
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeCenterLocation, 2, nameof(RoundedBoxInstance.Center)),
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeHalfExtentLocation, 2, nameof(RoundedBoxInstance.HalfExtent)),
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeCornerRadiiLocation, 4, nameof(RoundedBoxInstance.CornerRadii)),
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeFillColorLocation, 4, nameof(RoundedBoxInstance.FillColor)),
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeStrokeColorLocation, 4, nameof(RoundedBoxInstance.StrokeColor)),
        Attribute<RoundedBoxInstance>(RoundedBoxShaderBindings.AttributeStrokeWidthLocation, 1, nameof(RoundedBoxInstance.StrokeWidth)),
    ];

    private static IReadOnlyList<InstanceAttribute> CapsuleAttributes { get; } =
    [
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeStartPointLocation, 2, nameof(CapsuleInstance.Start)),
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeEndPointLocation, 2, nameof(CapsuleInstance.End)),
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeThicknessLocation, 1, nameof(CapsuleInstance.Thickness)),
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeFillColorLocation, 4, nameof(CapsuleInstance.FillColor)),
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeStrokeColorLocation, 4, nameof(CapsuleInstance.StrokeColor)),
        Attribute<CapsuleInstance>(CapsuleShaderBindings.AttributeStrokeWidthLocation, 1, nameof(CapsuleInstance.StrokeWidth)),
    ];

    private static IReadOnlyList<InstanceAttribute> QuadraticBezierAttributes { get; } =
    [
        Attribute<QuadraticBezierInstance>(QuadraticBezierShaderBindings.AttributeStartPointLocation, 2, nameof(QuadraticBezierInstance.Start)),
        Attribute<QuadraticBezierInstance>(QuadraticBezierShaderBindings.AttributeControlPointLocation, 2, nameof(QuadraticBezierInstance.Control)),
        Attribute<QuadraticBezierInstance>(QuadraticBezierShaderBindings.AttributeEndPointLocation, 2, nameof(QuadraticBezierInstance.End)),
        Attribute<QuadraticBezierInstance>(QuadraticBezierShaderBindings.AttributeThicknessLocation, 1, nameof(QuadraticBezierInstance.Thickness)),
        Attribute<QuadraticBezierInstance>(QuadraticBezierShaderBindings.AttributeColorLocation, 4, nameof(QuadraticBezierInstance.Color)),
    ];

    private static IReadOnlyList<InstanceAttribute> TexturedQuadAttributes { get; } =
    [
        Attribute<TexturedQuadInstance>(SpriteShaderBindings.AttributeDestinationLocation, 4, nameof(TexturedQuadInstance.Destination)),
        Attribute<TexturedQuadInstance>(SpriteShaderBindings.AttributeUvRectangleLocation, 4, nameof(TexturedQuadInstance.UvRectangle)),
        Attribute<TexturedQuadInstance>(SpriteShaderBindings.AttributeTintLocation, 4, nameof(TexturedQuadInstance.Tint)),
    ];

    private static IReadOnlyList<InstanceAttribute> TriangleAttributes { get; } =
    [
        Attribute<ColoredTriangleInstance>(TriangleShaderBindings.AttributeV0Location, 2, nameof(ColoredTriangleInstance.V0)),
        Attribute<ColoredTriangleInstance>(TriangleShaderBindings.AttributeV1Location, 2, nameof(ColoredTriangleInstance.V1)),
        Attribute<ColoredTriangleInstance>(TriangleShaderBindings.AttributeV2Location, 2, nameof(ColoredTriangleInstance.V2)),
        Attribute<ColoredTriangleInstance>(TriangleShaderBindings.AttributeColorLocation, 4, nameof(ColoredTriangleInstance.Color)),
    ];

    private static IReadOnlyList<InstanceAttribute> ArrowAttributes { get; } =
    [
        Attribute<ArrowheadInstance>(ArrowShaderBindings.AttributeTipLocation, 2, nameof(ArrowheadInstance.Tip)),
        Attribute<ArrowheadInstance>(ArrowShaderBindings.AttributeBaseLocation, 2, nameof(ArrowheadInstance.Base)),
        Attribute<ArrowheadInstance>(ArrowShaderBindings.AttributeHalfWidthLocation, 1, nameof(ArrowheadInstance.HalfWidth)),
        Attribute<ArrowheadInstance>(ArrowShaderBindings.AttributeColorLocation, 4, nameof(ArrowheadInstance.Color)),
    ];

    private static InstanceAttribute Attribute<T>(int location, int components, string field)
        where T : unmanaged
    {
        return new InstanceAttribute(checked((uint)location), components, Marshal.OffsetOf<T>(field).ToInt32());
    }

    private void DrawTexturedQuad(
        BatchKind kind,
        Texture2D texture,
        Rect destination,
        TextureRegion source,
        Color tint)
    {
        EnsureDrawing();
        ArgumentNullException.ThrowIfNull(texture);
        ValidateRectangle(destination);
        SwitchBatch(kind, texture);
        _texturedQuads.Add(new TexturedQuadInstance
        {
            Destination = new Vector4(destination.X, destination.Y, destination.Width, destination.Height),
            UvRectangle = new Vector4(source.Minimum.X, source.Minimum.Y, source.Maximum.X, source.Maximum.Y),
            Tint = tint.Vector4,
        });
    }

    private void SwitchBatch(BatchKind kind, Texture2D? texture)
    {
        if (_activeBatch == kind && ReferenceEquals(_activeTexture, texture))
        {
            return;
        }

        Flush();
        _activeBatch = kind;
        _activeTexture = texture;
    }

    private void Flush()
    {
        switch (_activeBatch)
        {
            case BatchKind.RoundedBox:
                _roundedBoxes.Draw(_programs.RoundedBox);
                break;
            case BatchKind.Capsule:
                _capsules.Draw(_programs.Capsule);
                break;
            case BatchKind.QuadraticBezier:
                _beziers.Draw(_programs.QuadraticBezier);
                break;
            case BatchKind.Glyph:
                _activeTexture!.Bind();
                _texturedQuads.Draw(_programs.Glyph);
                break;
            case BatchKind.Sprite:
                _activeTexture!.Bind();
                _texturedQuads.Draw(_programs.Sprite);
                break;
            case BatchKind.Arrow:
                _arrowheads.Draw(_programs.Arrow);
                break;
            case BatchKind.Triangle:
                _triangles.Draw(_programs.Triangle);
                break;
        }
    }

    private void EnsureDrawing()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_begun)
        {
            throw new InvalidOperationException("Begin must be called before drawing.");
        }
    }

    private static void ValidateRectangle(Rect rectangle)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rectangle), "Rectangle dimensions must be positive.");
        }
    }

    public Vector2 WindowToFramebuffer(Vector2 windowPosition)
    {
        return windowPosition * FramebufferSize / _viewport.WindowSize;
    }

    private enum BatchKind
    {
        None,
        RoundedBox,
        Capsule,
        QuadraticBezier,
        Glyph,
        Sprite,
        Arrow,
        Triangle,
    }

}
