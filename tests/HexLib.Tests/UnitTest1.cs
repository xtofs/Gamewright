
namespace HexLib.Tests;

public class CubeMathTests
{

    [Theory]
    [MemberData(nameof(GetIndices), 27)]
    private static void CubeFromIndex_RoundTripsThrough_IndexFromCube(int ix)
    {
        CubeMath.CubeFromIndex(ix, out var q, out var r, out var s);
        var actualIndex = CubeMath.IndexFromCube(q, r, s);

        Assert.Equal(ix, actualIndex); // $"Round trip failed for index {expectedIndex}: ({q}, {r}, {s}) maps back to {actualIndex}");
    }

    [Theory]
    [MemberData(nameof(GetCubeCoords), 7)]
    private static void IndexFromCube_RoundTripsThrough_CubeFromIndex(int q, int r, int s)
    {
        var ix = CubeMath.IndexFromCube(q, r, s);
        CubeMath.CubeFromIndex(ix, out var qa, out var ra, out var sa);


        Assert.Equal(q, qa);
        Assert.Equal(r, ra);
        Assert.Equal(s, sa);
    }

    public static TheoryData<int> GetIndices(int length)
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < length; i++)
        {
            data.Add(i);
        }
        return data;
    }

    public static TheoryData<int, int, int> GetCubeCoords(int radius)
    {
        var data = new TheoryData<int, int, int>();
        foreach (var ((q, r, s), i) in CubeMath.EnumerateGridCoords(radius))
        {
            data.Add(q, r, s);
        }
        return data;
    }
}

