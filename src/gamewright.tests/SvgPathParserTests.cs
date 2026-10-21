using Gamewright;

namespace Gamewright.Tests;

public class SvgPathParserTests
{
    private static PathOpcode[] Ops(byte[] data) => [.. data.Select(static b => (PathOpcode)b)];

    [Fact]
    public void ParsesAbsoluteLinesAndClose()
    {
        var result = SvgPathParser.Parse("M0 0 L10 0 L10 10 Z");

        Assert.Equal(
            [PathOpcode.MoveTo, PathOpcode.LineTo, PathOpcode.LineTo, PathOpcode.ClosePath],
            Ops(result.PathData));
        Assert.Equal([0f, 0, 10, 0, 10, 10], result.Geometry);
        Assert.True(result.Closed);
    }

    [Fact]
    public void RelativeCommandsAreResolvedToAbsoluteCoordinates()
    {
        var result = SvgPathParser.Parse("m1 1 l2 3 h4 v-1");

        Assert.Equal([1f, 1, 3, 4, 7, 4, 7, 3], result.Geometry);
        Assert.False(result.Closed);
    }

    [Fact]
    public void ExtraCoordinatePairsAfterMoveBecomeLines()
    {
        var result = SvgPathParser.Parse("M0 0 5 5 10 0");

        Assert.Equal([PathOpcode.MoveTo, PathOpcode.LineTo, PathOpcode.LineTo], Ops(result.PathData));
    }

    [Fact]
    public void SmoothCubicReflectsPreviousControlPoint()
    {
        var result = SvgPathParser.Parse("M0 0 C0 10 10 10 10 0 S20 -10 20 0");

        // Reflected first control of the S segment: 2 * (10, 0) - (10, 10) = (10, -10).
        Assert.Equal([10f, -10, 20, -10, 20, 0], result.Geometry.Skip(8).ToArray());
    }

    [Fact]
    public void ZeroRadiusArcBecomesLine()
    {
        var result = SvgPathParser.Parse("M0 0 A0 5 0 0 1 10 10");

        Assert.Equal([PathOpcode.MoveTo, PathOpcode.LineTo], Ops(result.PathData));
    }

    [Fact]
    public void ArcKeepsFlags()
    {
        var result = SvgPathParser.Parse("M0 0 A5 5 0 1 0 10 0");

        Assert.Equal(PathOpcode.ArcTo, (PathOpcode)result.PathData[1]);
        Assert.Equal([5f, 5, 0, 1, 0, 10, 0], result.Geometry.Skip(2).ToArray());
    }

    [Theory]
    [InlineData("M0 0 A5 5 0 011.5 1.5")]
    [InlineData("M0 0 A5 5 0 0 1 1.5 1.5")]
    [InlineData("M0 0 A5,5,0,0,1,1.5,1.5")]
    public void CompactArcFlagsParseLikeSpacedOnes(string path)
    {
        var result = SvgPathParser.Parse(path);

        Assert.Equal([5f, 5, 0, 0, 1, 1.5f, 1.5f], result.Geometry.Skip(2).ToArray());
    }

    [Fact]
    public void ConsecutiveCompactFlagsAreSplit()
    {
        var result = SvgPathParser.Parse("M0 0 a5 5 0 1110 0");

        Assert.Equal([5f, 5, 0, 1, 1, 10, 0], result.Geometry.Skip(2).ToArray());
    }

    [Theory]
    [InlineData("M5. 5. L10. 0.", new[] { 5f, 5, 10, 0 })]
    [InlineData("M.5.5L1.5.5", new[] { 0.5f, 0.5f, 1.5f, 0.5f })]
    public void NumbersMayEndOrStartWithADot(string path, float[] expected) =>
        Assert.Equal(expected, SvgPathParser.Parse(path).Geometry);

    [Theory]
    [InlineData("")]
    [InlineData("L0 0")]
    [InlineData("M0")]
    [InlineData("M0 0 L1")]
    [InlineData("M0 0 # 1 1")]
    [InlineData("M0 0 A5 5 0 2 0 10 0")]
    [InlineData("M0 0 A5 5 0 -1 0 10 0")]
    [InlineData("M0 0 A5 5 0 1")]
    [InlineData("M0 0 X1 1")]
    public void RejectsMalformedData(string path) =>
        Assert.Throws<FormatException>(() => SvgPathParser.Parse(path));
}
