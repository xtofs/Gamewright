namespace Gamewright.CodeGen;

using System.Text;

public static class ShaderSourceFlattener
{
    public static string Flatten(string shaderSource, IEnumerable<string> preludes)
    {
        ArgumentNullException.ThrowIfNull(shaderSource);
        ArgumentNullException.ThrowIfNull(preludes);

        var shaderLines = SplitLines(shaderSource);
        var versionIndex = shaderLines.FindIndex(line => line.TrimStart().StartsWith("#version", StringComparison.Ordinal));
        if (versionIndex < 0)
        {
            throw new InvalidOperationException("Shader source must contain a #version directive.");
        }

        var preludeList = preludes.ToArray();
        if (preludeList.Any(prelude => SplitLines(prelude).Any(line => line.TrimStart().StartsWith("#version", StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("Shader preludes must not contain a #version directive.");
        }

        var result = new StringBuilder();
        result.AppendLine(shaderLines[versionIndex]);
        foreach (var prelude in preludeList)
        {
            result.AppendLine(prelude.TrimEnd());
        }

        foreach (var line in shaderLines.Where((_, index) => index != versionIndex))
        {
            result.AppendLine(line);
        }

        return result.ToString();
    }

    private static List<string> SplitLines(string source)
    {
        return source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
    }
}
