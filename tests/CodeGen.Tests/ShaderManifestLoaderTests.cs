namespace CodeGen.Tests;

using Gamewright.CodeGen;

public sealed class ShaderManifestLoaderTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"Gamewright-{Guid.NewGuid():N}");

    [Fact]
    public void Load_ResolvesIncludesAndParsesBothStages()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "common.glsl"), "uniform float time;");
        File.WriteAllText(Path.Combine(directory, "shape.vert"), "#version 330 core\nlayout(location = 0) in vec2 position;");
        File.WriteAllText(Path.Combine(directory, "shape.frag"), "#version 330 core\nout vec4 color;");
        var manifestPath = Path.Combine(directory, "shaders.json");
        File.WriteAllText(
            manifestPath,
            """
            {
              "namespace": "Gamewright.Generated",
              "programs": [
                {
                  "name": "Shape",
                  "vertex": "shape.vert",
                  "fragment": "shape.frag",
                  "includes": ["common.glsl"]
                }
              ]
            }
            """);

        var input = ShaderManifestLoader.Load(manifestPath);

        Assert.Equal("Gamewright.Generated", input.Namespace);
        var program = Assert.Single(input.Programs);
        Assert.Equal("Shape", program.Name);
        Assert.Contains(program.Vertex.Declarations, declaration => declaration is VariableDeclaration { Name: "position" });
        Assert.Contains(program.Fragment.Declarations, declaration => declaration is VariableDeclaration { Name: "color" });
        Assert.Contains(program.Vertex.Declarations, declaration => declaration is VariableDeclaration { Name: "time" });
        Assert.Contains(program.Fragment.Declarations, declaration => declaration is VariableDeclaration { Name: "time" });
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
