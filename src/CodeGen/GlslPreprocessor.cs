namespace Gamewright.CodeGen;

using System.Text;
using System.Text.RegularExpressions;

public sealed record PreprocessedShader(
    string RuntimeSource,
    string ParseSource,
    IReadOnlyDictionary<string, int> IntegerDefines);

public static partial class GlslPreprocessor
{
    public static PreprocessedShader Process(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var defines = new Dictionary<string, int>(StringComparer.Ordinal);
        var parseSource = new StringBuilder();
        var conditionalDepth = 0;

        foreach (var line in source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (IsConditionalStart(trimmed))
            {
                conditionalDepth++;
                parseSource.AppendLine();
                continue;
            }

            if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
            {
                if (conditionalDepth == 0)
                {
                    throw new InvalidOperationException("Encountered #endif without a matching conditional directive.");
                }

                conditionalDepth--;
                parseSource.AppendLine();
                continue;
            }

            if (conditionalDepth > 0)
            {
                parseSource.AppendLine();
                continue;
            }

            var define = IntegerDefine.Match(line);
            if (define.Success)
            {
                var name = define.Groups["name"].Value;
                var value = int.Parse(define.Groups["value"].Value);
                if (!defines.TryAdd(name, value))
                {
                    throw new InvalidOperationException($"Integer define '{name}' is declared more than once.");
                }
            }

            parseSource.AppendLine(line);
        }

        if (conditionalDepth != 0)
        {
            throw new InvalidOperationException("Unterminated conditional preprocessor region.");
        }

        return new PreprocessedShader(source, parseSource.ToString(), defines);
    }

    private static bool IsConditionalStart(string line)
    {
        return line.StartsWith("#if ", StringComparison.Ordinal)
            || line.StartsWith("#if(", StringComparison.Ordinal)
            || line.StartsWith("#ifdef", StringComparison.Ordinal)
            || line.StartsWith("#ifndef", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*#define\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+(?<value>[1-9][0-9]*)\s*(?://.*)?$")]
    private static partial Regex IntegerDefine { get; }
}
