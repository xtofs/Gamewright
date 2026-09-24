namespace CodeGen.Tests;

using Gamewright.CodeGen;

public sealed class ParserTests
{
    [Fact]
    public void Parse_ReadsLayoutsAndArraysWithoutLeakingLayoutState()
    {
        const string source = """
            layout(std140, binding = 2) uniform mat4 transforms[4];
            uniform vec4 tint;
            uniform float weights[];
            """;

        var declarations = Parser.Parse(Tokenizer.Tokenize(source).ToArray());

        Assert.Collection(
            declarations,
            declaration =>
            {
                var variable = Assert.IsType<VariableDeclaration>(declaration);
                Assert.Equal(DeclarationKind.Uniform, variable.Kind);
                Assert.Equal("mat4", variable.Type.Value);
                Assert.Equal("transforms", variable.Name);
                Assert.Equal(new ArraySpecifier(4), variable.Array);
                Assert.Equal(
                    [new LayoutQualifier("std140", null), new LayoutQualifier("binding", "2")],
                    variable.Layout);
            },
            declaration =>
            {
                var variable = Assert.IsType<VariableDeclaration>(declaration);
                Assert.Equal("tint", variable.Name);
                Assert.Empty(variable.Layout);
                Assert.Null(variable.Array);
            },
            declaration =>
            {
                var variable = Assert.IsType<VariableDeclaration>(declaration);
                Assert.Equal(new ArraySpecifier(null), variable.Array);
            });
    }

    [Fact]
    public void Parse_SkipsFunctionParametersAndBodies()
    {
        const string source = """
            uniform float time;
            void update(out vec4 value)
            {
                value = vec4(time);
            }
            """;

        var declaration = Assert.IsType<VariableDeclaration>(
            Assert.Single(Parser.Parse(Tokenizer.Tokenize(source).ToArray())));

        Assert.Equal("time", declaration.Name);
    }

    [Fact]
    public void Parse_ReadsInterfaceBlockMembersAndInstance()
    {
        const string source = """
            layout(std430, binding = 3) buffer Particles
            {
                vec4 origin;
                float values[];
            } particles;
            """;

        var block = Assert.IsType<InterfaceBlockDeclaration>(
            Assert.Single(Parser.Parse(Tokenizer.Tokenize(source).ToArray())));

        Assert.Equal(DeclarationKind.Buffer, block.Kind);
        Assert.Equal("Particles", block.BlockName);
        Assert.Equal("particles", block.InstanceName);
        Assert.Equal("3", Assert.Single(block.Layout, item => item.Name == "binding").Value);
        Assert.Collection(
            block.Members,
            member => Assert.Equal("origin", member.Name),
            member => Assert.True(member.Array?.IsUnsized));
    }

    [Theory]
    [InlineData("uniform vec4 tint", "Expected ';'")]
    [InlineData("uniform vec4 tint[0];", "positive integer or an integer #define")]
    [InlineData("uniform vec4 @tint;", "Unexpected character")]
    [InlineData("uniform Values { float values[]; };", "final member of a buffer block")]
    [InlineData("buffer Values { float values[]; float tail; };", "final member of a buffer block")]
    public void Parse_RejectsMalformedDeclarations(string source, string expectedMessage)
    {
        var exception = Assert.Throws<GlslSyntaxException>(
            () => Parser.Parse(Tokenizer.Tokenize(source).ToArray()));

        Assert.Contains(expectedMessage, exception.Message);
    }
}
