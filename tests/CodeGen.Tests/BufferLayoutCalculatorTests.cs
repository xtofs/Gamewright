namespace CodeGen.Tests;

using Gamewright.CodeGen;

public sealed class BufferLayoutCalculatorTests
{
    [Theory]
    [InlineData("std140", "uniform", 16, 48)]
    [InlineData("std430", "buffer", 4, 16)]
    public void Calculate_AppliesArrayStrideRules(
        string standard,
        string declarationKind,
        int expectedStride,
        int expectedSize)
    {
        var block = ParseBlock($$"""
            layout({{standard}}) {{declarationKind}} Values
            {
                float samples[2];
                vec2 scale;
            };
            """);

        var layout = BufferLayoutCalculator.Calculate(block);

        Assert.Equal(expectedStride, layout.Members[0].TypeLayout.ArrayStride);
        Assert.Equal(expectedStride * 2, layout.Members[1].Offset);
        Assert.Equal(expectedSize, layout.Size);
    }

    [Fact]
    public void Calculate_PacksScalarIntoVec3Tail()
    {
        var block = ParseBlock("layout(std140) uniform Values { vec3 color; float alpha; };");

        var layout = BufferLayoutCalculator.Calculate(block);

        Assert.Equal(0, layout.Members[0].Offset);
        Assert.Equal(12, layout.Members[1].Offset);
        Assert.Equal(16, layout.Size);
    }

    [Fact]
    public void Calculate_UsesMatrixColumnStride()
    {
        var block = ParseBlock("layout(std140) uniform Values { mat3 transform; float alpha; };");

        var layout = BufferLayoutCalculator.Calculate(block);

        Assert.Equal(16, layout.Members[0].TypeLayout.MatrixStride);
        Assert.Equal(48, layout.Members[1].Offset);
        Assert.Equal(64, layout.Size);
    }

    [Fact]
    public void Calculate_ReportsTrailingUnsizedArrayStrideWithoutAddingItToSize()
    {
        var block = ParseBlock("layout(std430) buffer Values { vec4 origin; float samples[]; };");

        var layout = BufferLayoutCalculator.Calculate(block);

        Assert.Equal(16, layout.Members[1].Offset);
        Assert.Equal(4, layout.Members[1].TypeLayout.ArrayStride);
        Assert.Equal(16, layout.Size);
    }

    [Theory]
    [InlineData("layout(std430) uniform Values { float value; };", "cannot use std430")]
    [InlineData("uniform Values { float value; };", "exactly one")]
    public void Calculate_RejectsInvalidBlockStandards(string source, string expectedMessage)
    {
        var exception = Assert.Throws<GlslSyntaxException>(
            () => BufferLayoutCalculator.Calculate(ParseBlock(source)));

        Assert.Contains(expectedMessage, exception.Message);
    }

    private static InterfaceBlockDeclaration ParseBlock(string source)
    {
        return Assert.IsType<InterfaceBlockDeclaration>(
            Assert.Single(Parser.Parse(Tokenizer.Tokenize(source).ToArray())));
    }
}
