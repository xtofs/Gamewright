namespace Gamewright.CodeGen;

using System.Text.Json;
using System.Text.RegularExpressions;

public static partial class ShaderManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static ShaderGenerationInput Load(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        var fullManifestPath = Path.GetFullPath(manifestPath);
        var baseDirectory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidOperationException("The shader manifest must have a parent directory.");
        var manifest = JsonSerializer.Deserialize<ShaderManifest>(File.ReadAllText(fullManifestPath), JsonOptions)
            ?? throw new InvalidOperationException("The shader manifest is empty.");

        ValidateManifest(manifest);
        var programs = manifest.Programs
            .Select(program => LoadProgram(baseDirectory, program))
            .ToArray();
        return new ShaderGenerationInput(manifest.Namespace, programs);
    }

    private static ShaderProgramInput LoadProgram(string baseDirectory, ShaderProgramManifest program)
    {
        var commonPreludes = program.Includes
            .Select(path => File.ReadAllText(ResolvePath(baseDirectory, path)))
            .ToArray();
        var vertexPreludes = commonPreludes.Concat(program.VertexIncludes
            .Select(path => File.ReadAllText(ResolvePath(baseDirectory, path)))).ToArray();
        var fragmentPreludes = commonPreludes.Concat(program.FragmentIncludes
            .Select(path => File.ReadAllText(ResolvePath(baseDirectory, path)))).ToArray();
        return new ShaderProgramInput(
            program.Name,
            LoadStage(baseDirectory, program.Vertex, vertexPreludes),
            LoadStage(baseDirectory, program.Fragment, fragmentPreludes));
    }

    private static ProcessedShaderStage LoadStage(
        string baseDirectory,
        string relativePath,
        IReadOnlyList<string> preludes)
    {
        var path = ResolvePath(baseDirectory, relativePath);
        var flattened = ShaderSourceFlattener.Flatten(File.ReadAllText(path), preludes);
        var preprocessed = GlslPreprocessor.Process(flattened);
        var declarations = Parser.Parse(
            Tokenizer.Tokenize(preprocessed.ParseSource).ToArray(),
            preprocessed.IntegerDefines);
        return new ProcessedShaderStage(path, preprocessed, declarations);
    }

    private static string ResolvePath(string baseDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"Shader path '{relativePath}' must be relative to the manifest.");
        }

        return Path.GetFullPath(relativePath, baseDirectory);
    }

    private static void ValidateManifest(ShaderManifest manifest)
    {
        if (!NamespacePattern.IsMatch(manifest.Namespace))
        {
            throw new InvalidOperationException($"'{manifest.Namespace}' is not a valid C# namespace.");
        }

        if (manifest.Programs.Count == 0)
        {
            throw new InvalidOperationException("The shader manifest must define at least one program.");
        }

        var duplicateName = manifest.Programs
            .GroupBy(program => program.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"Shader program '{duplicateName}' is declared more than once.");
        }

        foreach (var program in manifest.Programs)
        {
            if (!IdentifierPattern.IsMatch(program.Name))
            {
                throw new InvalidOperationException($"'{program.Name}' is not a valid shader program name.");
            }

            if (string.IsNullOrWhiteSpace(program.Vertex) || string.IsNullOrWhiteSpace(program.Fragment))
            {
                throw new InvalidOperationException($"Shader program '{program.Name}' must define vertex and fragment stages.");
            }
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierPattern { get; }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex NamespacePattern { get; }
}
