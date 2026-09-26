namespace Gamewright.CodeGen;

using System.Text.Json;

public static partial class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: CodeGen <shader-manifest.json> <output-directory>");
            return 1;
        }

        try
        {
            var input = ShaderManifestLoader.Load(args[0]);
            var outputDirectory = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(outputDirectory);

            var outputPath = Path.Combine(outputDirectory, "ShaderBindings.g.cs");
            var bindings = CSharpBindingEmitter.Emit(input);

            // leave an unchanged file alone so it does not trigger recompiles or show up in git
            if (!File.Exists(outputPath) || File.ReadAllText(outputPath) != bindings)
            {
                File.WriteAllText(outputPath, bindings);
            }

            return 0;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or GlslSyntaxException)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }
}
