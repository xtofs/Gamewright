using Gamewright;

namespace Gamewright.Tests;

public class PathBuilderTests
{
    [Fact]
    public void MustStartWithMoveTo() =>
        Assert.Throws<InvalidOperationException>(() => new PathBuilder().LineTo(1, 1));

    [Fact]
    public void EmptyPathCannotBeBuilt() =>
        Assert.Throws<InvalidOperationException>(() => new PathBuilder().Build());

    [Fact]
    public void RejectsNonFiniteCoordinates() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathBuilder().MoveTo(float.NaN, 0));

    [Fact]
    public void RecordsOperationsAndGeometry()
    {
        var result = new PathBuilder().MoveTo(1, 2).LineTo(3, 4).QuadTo(5, 6, 7, 8).Close().Build();

        Assert.Equal(
            [PathOpcode.MoveTo, PathOpcode.LineTo, PathOpcode.QuadTo, PathOpcode.ClosePath],
            result.PathData.Select(static b => (PathOpcode)b));
        Assert.Equal([1f, 2, 3, 4, 5, 6, 7, 8], result.Geometry);
        Assert.True(result.Closed);
    }

    [Fact]
    public void DrawingAfterCloseReopensThePath()
    {
        var result = new PathBuilder().MoveTo(0, 0).LineTo(1, 1).Close().LineTo(2, 2).Build();

        Assert.False(result.Closed);
    }
}
