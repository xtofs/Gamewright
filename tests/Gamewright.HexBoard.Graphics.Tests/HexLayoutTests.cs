namespace Gamewright.HexBoard.Graphics.Tests;

using System.Numerics;
using Gamewright.HexBoard;

public class HexLayoutTests
{
    private const int Radius = 5;
    private const float Epsilon = 1e-3f;

    private static HexLayout CreateLayout(Vector2 position, Vector2 size)
    {
        var layout = new HexLayout(Radius);
        layout.Update(position, size);
        return layout;
    }

    public static TheoryData<float, float> AreaSizes() => new()
    {
        { 1600, 1200 },
        { 800, 1400 },
        { 1000, 1000 },
    };

    [Theory]
    [MemberData(nameof(AreaSizes))]
    public void TryGetHex_OfCenter_RoundTrips(float width, float height)
    {
        var layout = CreateLayout(new Vector2(30, 40), new Vector2(width, height));

        foreach (var hex in layout.GetGrid())
        {
            Assert.True(layout.TryGetHex(layout.GetCenter(hex), out var actual));
            Assert.Equal(hex, actual);
        }
    }

    [Fact]
    public void TryGetHex_NearCorners_StaysInHexagon()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));

        foreach (var hex in layout.GetGrid())
        {
            var center = layout.GetCenter(hex);
            var corners = layout.GetHexCorners(hex);
            foreach (var corner in (ReadOnlySpan<Vector2>)corners)
            {
                var inside = Vector2.Lerp(center, corner, 0.95f);
                Assert.True(layout.TryGetHex(inside, out var actual));
                Assert.Equal(hex, actual);
            }
        }
    }

    [Fact]
    public void TryGetHex_OutsideGrid_ReturnsFalse()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));

        Assert.False(layout.TryGetHex(new Vector2(2, 2), out _));
    }

    [Fact]
    public void TryGetHex_BeforeUpdate_ReturnsFalse()
    {
        var layout = new HexLayout(Radius);

        Assert.False(layout.TryGetHex(Vector2.Zero, out _));
    }

    [Fact]
    public void Corners_AreAtHexagonRadius_WithBottomVertexFirst()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));
        var hex = new Coordinate(2, -3, 1);
        var center = layout.GetCenter(hex);
        var corners = layout.GetHexCorners(hex);

        foreach (var corner in (ReadOnlySpan<Vector2>)corners)
        {
            Assert.Equal(layout.HexagonRadius, Vector2.Distance(center, corner), Epsilon);
        }

        // pointy-top: the first corner is straight below the center (y points down)
        Assert.Equal(center.X, corners[0].X, Epsilon);
        Assert.Equal(center.Y + layout.HexagonRadius, corners[0].Y, Epsilon);
    }

    [Fact]
    public void Neighbors_AreOneHexagonWidthApart()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));
        var hex = new Coordinate(1, -2, 1);

        foreach (var direction in Directions.All)
        {
            var distance = Vector2.Distance(layout.GetCenter(hex), layout.GetCenter(hex + direction));
            Assert.Equal(MathF.Sqrt(3) * layout.HexagonRadius, distance, Epsilon);
        }
    }

    [Fact]
    public void Directions_PointTheWayTheyAreNamed()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));
        var origin = layout.GetCenter(Coordinate.Center);

        Vector2 Step(Direction d) => layout.GetCenter(Coordinate.Center + d) - origin;

        Assert.True(Step(Direction.East).X > 0 && MathF.Abs(Step(Direction.East).Y) < Epsilon);
        Assert.True(Step(Direction.West).X < 0 && MathF.Abs(Step(Direction.West).Y) < Epsilon);
        Assert.True(Step(Direction.NorthEast) is { X: > 0, Y: < 0 });
        Assert.True(Step(Direction.NorthWest) is { X: < 0, Y: < 0 });
        Assert.True(Step(Direction.SouthEast) is { X: > 0, Y: > 0 });
        Assert.True(Step(Direction.SouthWest) is { X: < 0, Y: > 0 });
    }

    [Theory]
    [MemberData(nameof(AreaSizes))]
    public void Update_FitsGridExactly(float width, float height)
    {
        var position = new Vector2(30, 40);
        var size = new Vector2(width, height);
        var layout = CreateLayout(position, size);

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var hex in layout.GetGrid())
        {
            var corners = layout.GetHexCorners(hex);
            foreach (var corner in (ReadOnlySpan<Vector2>)corners)
            {
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }
        }

        // inside the area ...
        Assert.True(min.X >= position.X - Epsilon && min.Y >= position.Y - Epsilon);
        Assert.True(max.X <= position.X + size.X + Epsilon && max.Y <= position.Y + size.Y + Epsilon);

        // ... centered ...
        Assert.Equal(position.X + size.X / 2, (min.X + max.X) / 2, Epsilon);
        Assert.Equal(position.Y + size.Y / 2, (min.Y + max.Y) / 2, Epsilon);

        // ... and touching the area on at least one axis
        var touchesX = MathF.Abs(max.X - min.X - size.X) < Epsilon;
        var touchesY = MathF.Abs(max.Y - min.Y - size.Y) < Epsilon;
        Assert.True(touchesX || touchesY);

        // the extent matches the advertised aspect ratio
        Assert.Equal(HexLayout.AspectRatio(Radius), (max.X - min.X) / (max.Y - min.Y), Epsilon);
    }

    [Fact]
    public void InscribedSquare_IsInsideHexagon()
    {
        var layout = CreateLayout(Vector2.Zero, new Vector2(1600, 1200));
        var hex = new Coordinate(-2, 3, -1);
        var (position, size) = layout.GetInscribedSquare(hex);

        // shrink slightly: the square touches the hexagon's sides
        var corners = new[] { position, position + size * Vector2.UnitX, position + size, position + size * Vector2.UnitY };
        var center = layout.GetCenter(hex);
        foreach (var corner in corners)
        {
            Assert.True(layout.TryGetHex(Vector2.Lerp(center, corner, 0.99f), out var actual));
            Assert.Equal(hex, actual);
        }

        Assert.Equal(center, position + size / 2);
    }

    [Theory]
    [InlineData(0.4f, -0.3f, -0.1f, 0, 0, 0)]
    [InlineData(1.1f, -0.6f, -0.5f, 1, -1, 0)]
    [InlineData(-0.2f, 0.9f, -0.7f, 0, 1, -1)]
    public void CubeRound_RoundsToNearestHexagon(float q, float r, float s, int eq, int er, int es)
    {
        Assert.Equal(new Coordinate(eq, er, es), HexLayout.CubeRound(q, r, s));
    }
}
