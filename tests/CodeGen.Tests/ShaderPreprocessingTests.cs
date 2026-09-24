namespace CodeGen.Tests;

using Gamewright.CodeGen;

public sealed class ShaderPreprocessingTests
{
    [Fact]
    public void Flatten_PreservesVersionAsFirstLineAndPreludeOrder()
    {
        const string shader = """
            // shader comment
            #version 330 core
            void main() { }
            """;

        var flattened = ShaderSourceFlattener.Flatten(shader, ["uniform float time;", "uniform vec2 size;"]);
        var lines = flattened.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("#version 330 core", lines[0].Trim());
        Assert.Equal("uniform float time;", lines[1]);
        Assert.Equal("uniform vec2 size;", lines[2]);
    }

    [Fact]
    public void Process_CollectsIntegerDefinesAndSkipsConditionalDeclarations()
    {
        const string source = """
            #version 330 core
            #define LIGHT_COUNT 4
            uniform vec4 lights[LIGHT_COUNT];
            #ifdef OPTIONAL_FEATURE
            uniform float optionalValue;
            #endif
            uniform float time;
            """;

        var preprocessed = GlslPreprocessor.Process(source);
        var declarations = Parser.Parse(
            Tokenizer.Tokenize(preprocessed.ParseSource).ToArray(),
            preprocessed.IntegerDefines);

        Assert.Equal(source, preprocessed.RuntimeSource);
        Assert.Equal(4, preprocessed.IntegerDefines["LIGHT_COUNT"]);
        Assert.Collection(
            declarations.Cast<VariableDeclaration>(),
            declaration => Assert.Equal(new ArraySpecifier(4), declaration.Array),
            declaration => Assert.Equal("time", declaration.Name));
    }

    [Theory]
    [InlineData("#endif", "without a matching")]
    [InlineData("#ifdef VALUE\nuniform float value;", "Unterminated")]
    public void Process_RejectsUnbalancedConditionals(string source, string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlslPreprocessor.Process(source));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Flatten_RejectsVersionDirectiveInPrelude()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ShaderSourceFlattener.Flatten("#version 330 core", ["#version 330 core"]));

        Assert.Contains("must not contain", exception.Message);
    }
}
