namespace Gamewright.Tests;


using System.Numerics;
using Gamewright.Geometry;

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
}
