namespace CodeGen.Tests;

using Gamewright.CodeGen;

public sealed class CSharpBindingEmitterTests
{
    [Fact]
    public void Emit_WritesSourcesAndTypedContractConstants()
    {
        var location = new SourceLocation(0, 1, 1);
        var vertexDeclarations = new Declaration[]
        {
            new VariableDeclaration(
                DeclarationKind.Input,
                new GlslTypeName("vec2"),
                "position",
                [new LayoutQualifier("location", "0")],
                null,
                location),
            new VariableDeclaration(
                DeclarationKind.Uniform,
                new GlslTypeName("vec4"),
                "tint",
                [],
                null,
                location),
            new InterfaceBlockDeclaration(
                DeclarationKind.Uniform,
                "Frame",
                [new BlockMember(new GlslTypeName("mat4"), "projection", null, location)],
                null,
                [new LayoutQualifier("std140", null), new LayoutQualifier("binding", "0")],
                null,
                location),
        };
        var input = new ShaderGenerationInput(
            "Gamewright.Generated",
            [new ShaderProgramInput(
                "Shape",
                Stage("#version 330 core\n", vertexDeclarations),
                Stage("#version 330 core\n", []))]);

        var source = CSharpBindingEmitter.Emit(input);

        Assert.Contains("namespace Gamewright.Generated;", source);
        Assert.Contains("internal static class ShapeShaderBindings", source);
        Assert.Contains("public const int AttributePositionLocation = 0;", source);
        Assert.Contains("public const string UniformTintName = \"tint\";", source);
        Assert.Contains("public const string FrameBlockName = \"Frame\";", source);
        Assert.Contains("public const int FrameBinding = 0;", source);
        Assert.Contains("public const string VertexSource", source);
    }

    [Fact]
    public void Emit_RejectsConflictingUniformTypes()
    {
        var location = new SourceLocation(0, 1, 1);
        var input = new ShaderGenerationInput(
            "Gamewright.Generated",
            [
                new ShaderProgramInput(
                    "First",
                    Stage("", [Uniform("tint", "vec3", location)]),
                    Stage("", [])),
                new ShaderProgramInput(
                    "Second",
                    Stage("", [Uniform("tint", "vec4", location)]),
                    Stage("", [])),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => CSharpBindingEmitter.Emit(input));

        Assert.Contains("tint", exception.Message);
    }

    [Fact]
    public void Emit_AssignsBindingWhenPortableShaderOmitsIt()
    {
        var location = new SourceLocation(0, 1, 1);
        var block = new InterfaceBlockDeclaration(
            DeclarationKind.Uniform,
            "Frame",
            [new BlockMember(new GlslTypeName("mat4"), "projection", null, location)],
            null,
            [new LayoutQualifier("std140", null)],
            null,
            location);
        var input = new ShaderGenerationInput(
            "Gamewright.Generated",
            [new ShaderProgramInput("Shape", Stage("", [block]), Stage("", []))]);

        var source = CSharpBindingEmitter.Emit(input);

        Assert.Contains("public const int FrameBinding = 0;", source);
    }

    private static ProcessedShaderStage Stage(string source, IReadOnlyList<Declaration> declarations)
    {
        return new ProcessedShaderStage(
            "shader.glsl",
            new PreprocessedShader(source, source, new Dictionary<string, int>()),
            declarations);
    }

    private static VariableDeclaration Uniform(string name, string type, SourceLocation location)
    {
        return new VariableDeclaration(
            DeclarationKind.Uniform,
            new GlslTypeName(type),
            name,
            [],
            null,
            location);
    }
}
