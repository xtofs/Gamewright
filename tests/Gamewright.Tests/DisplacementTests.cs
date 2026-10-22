using System.Numerics;
using Gamewright;

namespace Gamewright.Tests;

public class DisplacementTests
{
    [Fact]
    public void AddsSubtractsNegatesAndScales()
    {
        var a = new Displacement(1, 2);
        var b = new Displacement(3, 5);

        Assert.Equal(new Displacement(4, 7), a + b);
        Assert.Equal(new Displacement(2, 3), b - a);
        Assert.Equal(new Displacement(-1, -2), -a);
        Assert.Equal(new Displacement(2, 4), a * 2);
        Assert.Equal(new Displacement(2, 4), 2 * a);
        Assert.Equal(new Displacement(0.5f, 1), a / 2);
    }

    [Fact]
    public void LengthIsEuclidean() => Assert.Equal(5f, new Displacement(3, 4).Length);

    [Fact]
    public void NormalizedHasLengthOne()
    {
        var unit = new Displacement(3, 4).Normalized;

        Assert.Equal(0.6f, unit.X, 5);
        Assert.Equal(0.8f, unit.Y, 5);
        Assert.Equal(1f, unit.Length, 5);
    }

    [Fact]
    public void NormalizingZeroLeavesZero() => Assert.Equal(Displacement.Zero, Displacement.Zero.Normalized);

    [Fact]
    public void TransformAppliesRotationButNotTranslation()
    {
        var matrix = Matrix3x2.CreateRotation(MathF.PI / 2) * Matrix3x2.CreateTranslation(100, 100);

        var rotated = new Displacement(1, 0).Transform(matrix);

        Assert.Equal(0f, rotated.X, 4);
        Assert.Equal(1f, rotated.Y, 4);
    }

    [Fact]
    public void ConvertsToAndFromVector2()
    {
        Assert.Equal(new Vector2(1, 2), (Vector2)new Displacement(1, 2));
        Assert.Equal(new Displacement(3, 4), (Displacement)new Vector2(3, 4));
    }
}
