namespace Gamewright.CodeGen;

using System.Text.RegularExpressions;

public static partial class Tokenizer
{
  public static IEnumerable<Token> Tokenize(string source)
  {
    ArgumentNullException.ThrowIfNull(source);
    var offset = 0;
    var line = 1;
    var column = 1;
    var match = GlslToken.Match(source);

    while (match.Success)
    {
      var location = new SourceLocation(offset, line, column);
      var group = match.Groups.Cast<Group>().Skip(1).Single(group => group.Success);
      if (group.Name is not ("whitespace" or "comment"))
      {
        var type = Enum.Parse<TokenType>(group.Name, ignoreCase: true);
        yield return new Token(type, group.Value, location);
      }

      foreach (var character in match.Value)
      {
        offset++;
        if (character == '\n')
        {
          line++;
          column = 1;
        }
        else
        {
          column++;
        }
      }

      match = match.NextMatch();
    }

    if (offset != source.Length)
    {
      throw new GlslSyntaxException($"Unexpected character '{source[offset]}'", new SourceLocation(offset, line, column));
    }
  }

  [GeneratedRegex(
    """
        \G(?:
            (?<whitespace>\s+)
          | (?<comment>//[^\n]* | /\*[\s\S]*?\*/)
          | (?<preprocessor>\#[^\n]*)
          | (?<Type>
                \b(?:
                    void | bool | int | uint | float | double | atomic_uint
                  | [bdiu]?vec[234]
                  | d?mat[234](?:x[234])?
                  | [iu]?sampler(?:1D|2D|3D|Cube|2DRect|1DArray|2DArray|CubeArray|Buffer|2DMS|2DMSArray)
                  | sampler(?:1D|2D|1DArray|2DArray|Cube|CubeArray|2DRect)Shadow
                  | [iu]?image(?:1D|2D|3D|Cube|2DRect|1DArray|2DArray|CubeArray|Buffer|2DMS|2DMSArray)
                  | [iu]?subpassInput(?:MS)?
                )\b
            )
          | (?<Keyword>
                \b(?:
                    const | uniform | buffer | shared | attribute | varying
                  | coherent | volatile | restrict | readonly | writeonly
                  | layout | centroid | flat | smooth | noperspective
                  | patch | sample | invariant | precise
                  | break | continue | do | for | while | switch | case | default
                  | if | else | discard | return
                  | subroutine | in | out | inout | struct
                  | true | false
                  | lowp | mediump | highp | precision
                )\b
            )
          | (?<Identifier>[A-Za-z_][A-Za-z0-9_]*)
          | (?<FloatingLiteral>
                (?: [0-9]+\.[0-9]* | \.[0-9]+ ) (?:[eE][+-]?[0-9]+)? (?:[fF]|lf|LF)?
              | [0-9]+ (?: [eE][+-]?[0-9]+ (?:[fF]|lf|LF)? | (?:[fF]|lf|LF) )
            )
          | (?<IntegerLiteral>
                0[xX][0-9a-fA-F]+[uU]?
              | 0[0-7]*[uU]?
              | [1-9][0-9]*[uU]?
            )
          | (?<operator>
                <<= | >>=
              | \+\+ | -- | << | >> | <= | >= | == | != | && | \|\| | \^\^
              | \*= | /= | \+= | -= | %= | &= | \^= | \|=
              | [-+*/%<>&^|~!=?:.,]
            )
          | (?<punctuation>[;{}()\[\]])
        )
        """,
    RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
  private static partial Regex GlslToken { get; }
}
