namespace Gamewright.CodeGen;

public sealed record ShaderManifest
{
    public required string Namespace { get; init; }

    public required IReadOnlyList<ShaderProgramManifest> Programs { get; init; }
}

public sealed record ShaderProgramManifest
{
    public required string Name { get; init; }

    public required string Vertex { get; init; }

    public required string Fragment { get; init; }

    public IReadOnlyList<string> Includes { get; init; } = [];

    public IReadOnlyList<string> VertexIncludes { get; init; } = [];

    public IReadOnlyList<string> FragmentIncludes { get; init; } = [];
}

public sealed record ProcessedShaderStage(
    string Path,
    PreprocessedShader Source,
    IReadOnlyList<Declaration> Declarations);

public sealed record ShaderProgramInput(
    string Name,
    ProcessedShaderStage Vertex,
    ProcessedShaderStage Fragment);

public sealed record ShaderGenerationInput(
    string Namespace,
    IReadOnlyList<ShaderProgramInput> Programs);
