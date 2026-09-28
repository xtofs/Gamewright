namespace Gamewright.Graphics.Tests;


using System.Numerics;
using Gamewright.Graphics.Geometry;

public sealed class GeometryBuilderTests
{
    [Fact]
    public void CreateRoundedBox_MapsRectangleAndCornerOrderToShaderInstance()
    {
        var instance = GeometryBuilder.CreateRoundedBox(
            new Rect(10, 20, 100, 60),
            new CornerRadii(1, 2, 3, 4),
            Colors.White,
            new Stroke(Colors.Yellow, 5));

        Assert.Equal(new Vector2(60, 50), instance.Center);
        Assert.Equal(new Vector2(50, 30), instance.HalfExtent);
        Assert.Equal(new Vector4(3, 2, 1, 4), instance.CornerRadii);
        Assert.Equal(5, instance.StrokeWidth);
    }

    [Fact]
    public void CreatePolyline_SkipsDegenerateSegmentsAndTheirJoins()
    {
        ReadOnlySpan<Vector2> points =
        [
            new Vector2(0, 0),
            new Vector2(10, 0),
            new Vector2(10, 0),
            new Vector2(20, 10),
        ];

        var instances = GeometryBuilder.CreatePolyline(points, 6, Colors.White);

        Assert.Equal(2, instances.Segments.Count);
        Assert.Empty(instances.Joins);
    }

    [Fact]
    public void CreatePolyline_AddsRoundJoinForConnectedSegments()
    {
        ReadOnlySpan<Vector2> points =
        [new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)];

        var instances = GeometryBuilder.CreatePolyline(points, 8, Colors.White);

        Assert.Equal(2, instances.Segments.Count);
        var join = Assert.Single(instances.Joins);
        Assert.Equal(new Vector2(10, 0), join.Center);
        Assert.Equal(new Vector2(4, 4), join.HalfExtent);
        Assert.Equal(new Vector4(4), join.CornerRadii);
    }

    [Fact]
    public void CreateSpline_RunsFromFirstPointThroughMidpointsToLastPoint()
    {
        // two hex centers apart by 120°: a turn like a path through three hexagons
        ReadOnlySpan<Vector2> points =
        [new Vector2(0, 0), new Vector2(20, 0), new Vector2(30, 17)];

        var pieces = GeometryBuilder.CreateSpline(points, 4, Colors.White);

        Assert.Equal(3, pieces.Count);
        Assert.Equal(new Vector2(0, 0), pieces[0].Start);
        Assert.Equal(new Vector2(10, 0), pieces[0].End);
        Assert.Equal(new Vector2(10, 0), pieces[1].Start);
        Assert.Equal(new Vector2(20, 0), pieces[1].Control);
        Assert.Equal(new Vector2(25, 8.5f), pieces[1].End);
        Assert.Equal(new Vector2(25, 8.5f), pieces[2].Start);
        Assert.Equal(new Vector2(30, 17), pieces[2].End);
    }

    [Fact]
    public void CreateSpline_IsTangentToThePolygonAtEachMidpoint()
    {
        ReadOnlySpan<Vector2> points =
        [new Vector2(0, 0), new Vector2(20, 0), new Vector2(30, 17), new Vector2(50, 17)];

        var pieces = GeometryBuilder.CreateSpline(points, 4, Colors.White);

        for (var i = 1; i < pieces.Count; i++)
        {
            var incoming = Vector2.Normalize(pieces[i - 1].End - pieces[i - 1].Control);
            var outgoing = Vector2.Normalize(pieces[i].Control - pieces[i].Start);
            Assert.Equal(pieces[i - 1].End, pieces[i].Start);
            Assert.True(Vector2.Distance(incoming, outgoing) < 1e-5f, $"kink at piece {i}");
        }
    }

    [Fact]
    public void CreateSpline_OfTwoPoints_IsAStraightLineThroughTheMidpoint()
    {
        ReadOnlySpan<Vector2> points = [new Vector2(0, 0), new Vector2(10, 0)];

        var pieces = GeometryBuilder.CreateSpline(points, 4, Colors.White);

        Assert.Equal(2, pieces.Count);
        Assert.Equal(new Vector2(0, 0), pieces[0].Start);
        Assert.Equal(new Vector2(2.5f, 0), pieces[0].Control);
        Assert.Equal(new Vector2(5, 0), pieces[0].End);
        Assert.Equal(new Vector2(5, 0), pieces[1].Start);
        Assert.Equal(new Vector2(7.5f, 0), pieces[1].Control);
        Assert.Equal(new Vector2(10, 0), pieces[1].End);
    }

    [Fact]
    public void CreateSpline_OfFewerThanTwoPoints_IsEmpty()
    {
        Assert.Empty(GeometryBuilder.CreateSpline([new Vector2(1, 2)], 4, Colors.White));
    }

    [Fact]
    public void CreateQuadraticBezier_MovesCollinearControlPointToTheMiddle()
    {
        var piece = GeometryBuilder.CreateQuadraticBezier(new Vector2(0, 0), new Vector2(9, 9), new Vector2(10, 10), 4, Colors.White);

        Assert.Equal(new Vector2(5, 5), piece.Control);
    }

    [Fact]
    public void CreateCapsule_WidensSubPixelLinesAndFadesThemByTheSameFactor()
    {
        var instance = GeometryBuilder.CreateCapsule(
            Vector2.Zero, new Vector2(10, 0), 0.25f, Colors.White, new Stroke(Colors.White.WithAlpha(0.5f), 0.5f));

        Assert.Equal(GeometryBuilder.MinimumWidth, instance.Thickness);
        Assert.Equal(0.25f, instance.FillColor.W);
        Assert.Equal(GeometryBuilder.MinimumWidth, instance.StrokeWidth);
        Assert.Equal(0.25f, instance.StrokeColor.W);
    }

    [Fact]
    public void CreateCapsule_KeepsLinesAtOrAboveTheMinimumWidth()
    {
        var instance = GeometryBuilder.CreateCapsule(Vector2.Zero, new Vector2(10, 0), 3, Colors.White, default);

        Assert.Equal(3, instance.Thickness);
        Assert.Equal(1, instance.FillColor.W);
        Assert.Equal(0, instance.StrokeWidth);
    }

    [Fact]
    public void CreateRoundedBox_WidensSubPixelStroke()
    {
        var instance = GeometryBuilder.CreateRoundedBox(
            new Rect(0, 0, 10, 10), default, Colors.Transparent, new Stroke(Colors.Red, 0.5f));

        Assert.Equal(GeometryBuilder.MinimumWidth, instance.StrokeWidth);
        Assert.Equal(0.5f, instance.StrokeColor.W);
    }

    [Fact]
    public void CreatePolyline_WidensSubPixelSegmentsAndJoinsAlike()
    {
        ReadOnlySpan<Vector2> points = [new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)];

        var instances = GeometryBuilder.CreatePolyline(points, 0.5f, Colors.White);

        Assert.All(instances.Segments, s => Assert.Equal((GeometryBuilder.MinimumWidth, 0.5f), (s.Thickness, s.FillColor.W)));
        var join = Assert.Single(instances.Joins);
        Assert.Equal(new Vector2(GeometryBuilder.MinimumWidth / 2), join.HalfExtent);
        Assert.Equal(0.5f, join.FillColor.W);
    }

    [Fact]
    public void CreateQuadraticBezier_WidensSubPixelCurves()
    {
        var instance = GeometryBuilder.CreateQuadraticBezier(
            Vector2.Zero, new Vector2(5, 5), new Vector2(10, 0), 0.5f, Colors.White);

        Assert.Equal(GeometryBuilder.MinimumWidth, instance.Thickness);
        Assert.Equal(0.5f, instance.Color.W);
    }
}
