namespace Gamewright.CodeGen;

public static class Parser
{
    public static IReadOnlyList<Declaration> Parse(IReadOnlyList<Token> tokens)
    {
        return Parse(tokens, new Dictionary<string, int>(StringComparer.Ordinal));
    }

    public static IReadOnlyList<Declaration> Parse(
        IReadOnlyList<Token> tokens,
        IReadOnlyDictionary<string, int> integerDefines)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(integerDefines);
        var declarations = new List<Declaration>();
        IReadOnlyList<LayoutQualifier> pendingLayout = [];
        var index = 0;

        while (index < tokens.Count)
        {
            var token = tokens[index];
            if (token is { Type: TokenType.Keyword, Value: "layout" })
            {
                pendingLayout = ParseLayout(tokens, ref index);
            }
            else if (TryGetDeclarationKind(token, out var kind))
            {
                declarations.Add(ParseDeclaration(tokens, ref index, kind, pendingLayout, integerDefines));
                pendingLayout = [];
            }
            else if (token is { Type: TokenType.Punctuation, Value: "{" })
            {
                SkipBalanced(tokens, ref index, "{", "}");
                pendingLayout = [];
            }
            else if (token is { Type: TokenType.Punctuation, Value: "(" })
            {
                SkipBalanced(tokens, ref index, "(", ")");
            }
            else
            {
                index++;
            }
        }

        return declarations;
    }

    private static Declaration ParseDeclaration(
        IReadOnlyList<Token> tokens,
        ref int index,
        DeclarationKind kind,
        IReadOnlyList<LayoutQualifier> layout,
        IReadOnlyDictionary<string, int> integerDefines)
    {
        var declarationToken = tokens[index++];
        var typeToken = Require(tokens, ref index, [TokenType.Type, TokenType.Identifier], "Expected a GLSL type");

        if (Is(tokens, index, TokenType.Punctuation, "{"))
        {
            if (kind is not (DeclarationKind.Uniform or DeclarationKind.Buffer))
            {
                throw new GlslSyntaxException("Only uniform and buffer declarations can be interface blocks", declarationToken.Location);
            }

            return ParseInterfaceBlock(tokens, ref index, kind, typeToken, layout, declarationToken.Location, integerDefines);
        }

        var nameToken = Require(tokens, ref index, [TokenType.Identifier], "Expected a declaration name");
        var array = ParseOptionalArray(tokens, ref index, integerDefines);

        RequireValue(tokens, ref index, TokenType.Punctuation, ";", "Expected ';' after declaration");
        return new VariableDeclaration(
            kind,
            new GlslTypeName(typeToken.Value),
            nameToken.Value,
            layout,
            array,
            declarationToken.Location);
    }

    private static InterfaceBlockDeclaration ParseInterfaceBlock(
        IReadOnlyList<Token> tokens,
        ref int index,
        DeclarationKind kind,
        Token blockName,
        IReadOnlyList<LayoutQualifier> layout,
        SourceLocation location,
        IReadOnlyDictionary<string, int> integerDefines)
    {
        index++;
        var members = new List<BlockMember>();
        while (!Is(tokens, index, TokenType.Punctuation, "}"))
        {
            var type = Require(tokens, ref index, [TokenType.Type, TokenType.Identifier], "Expected a block member type");
            var name = Require(tokens, ref index, [TokenType.Identifier], "Expected a block member name");
            var array = ParseOptionalArray(tokens, ref index, integerDefines);
            RequireValue(tokens, ref index, TokenType.Punctuation, ";", "Expected ';' after block member");
            members.Add(new BlockMember(new GlslTypeName(type.Value), name.Value, array, type.Location));
        }

        RequireValue(tokens, ref index, TokenType.Punctuation, "}", "Expected '}' after interface block");
        string? instanceName = null;
        ArraySpecifier? instanceArray = null;
        if (index < tokens.Count && tokens[index].Type == TokenType.Identifier)
        {
            instanceName = tokens[index++].Value;
            instanceArray = ParseOptionalArray(tokens, ref index, integerDefines);
        }

        RequireValue(tokens, ref index, TokenType.Punctuation, ";", "Expected ';' after interface block");
        ValidateBlockMembers(kind, members);
        return new InterfaceBlockDeclaration(
            kind,
            blockName.Value,
            members,
            instanceName,
            layout,
            instanceArray,
            location);
    }

    private static ArraySpecifier? ParseOptionalArray(
        IReadOnlyList<Token> tokens,
        ref int index,
        IReadOnlyDictionary<string, int> integerDefines)
    {
        ArraySpecifier? array = null;

        if (Is(tokens, index, TokenType.Punctuation, "["))
        {
            index++;
            if (Is(tokens, index, TokenType.Punctuation, "]"))
            {
                array = new ArraySpecifier(null);
            }
            else
            {
                var lengthToken = Require(
                    tokens,
                    ref index,
                    [TokenType.IntegerLiteral, TokenType.Identifier],
                    "Expected an integer array length");
                var hasLength = int.TryParse(lengthToken.Value, out var length)
                    || integerDefines.TryGetValue(lengthToken.Value, out length);
                if (!hasLength || length <= 0)
                {
                    throw new GlslSyntaxException(
                        "Array length must be a positive integer or an integer #define",
                        lengthToken.Location);
                }

                array = new ArraySpecifier(length);
            }

            RequireValue(tokens, ref index, TokenType.Punctuation, "]", "Expected ']' after array length");
        }

        return array;
    }

    private static void ValidateBlockMembers(DeclarationKind kind, IReadOnlyList<BlockMember> members)
    {
        for (var index = 0; index < members.Count; index++)
        {
            var member = members[index];
            if (member.Array is not { IsUnsized: true })
            {
                continue;
            }

            if (kind != DeclarationKind.Buffer || index != members.Count - 1)
            {
                throw new GlslSyntaxException(
                    "An unsized array is only valid as the final member of a buffer block",
                    member.Location);
            }
        }
    }

    private static IReadOnlyList<LayoutQualifier> ParseLayout(IReadOnlyList<Token> tokens, ref int index)
    {
        index++;
        RequireValue(tokens, ref index, TokenType.Punctuation, "(", "Expected '(' after layout");
        var qualifiers = new List<LayoutQualifier>();

        while (!Is(tokens, index, TokenType.Punctuation, ")"))
        {
            var name = Require(tokens, ref index, [TokenType.Identifier], "Expected a layout qualifier");
            string? value = null;
            if (Is(tokens, index, TokenType.Operator, "="))
            {
                index++;
                value = Require(tokens, ref index, [TokenType.Identifier, TokenType.IntegerLiteral], "Expected a layout value").Value;
            }

            qualifiers.Add(new LayoutQualifier(name.Value, value));
            if (!Is(tokens, index, TokenType.Operator, ","))
            {
                break;
            }

            index++;
        }

        RequireValue(tokens, ref index, TokenType.Punctuation, ")", "Expected ')' after layout qualifiers");
        return qualifiers;
    }

    private static void SkipBalanced(IReadOnlyList<Token> tokens, ref int index, string opening, string closing)
    {
        var start = tokens[index];
        var depth = 0;
        while (index < tokens.Count)
        {
            var token = tokens[index++];
            if (token.Type == TokenType.Punctuation && token.Value == opening)
            {
                depth++;
            }
            else if (token.Type == TokenType.Punctuation && token.Value == closing)
            {
                depth--;
                if (depth == 0)
                {
                    return;
                }
            }
        }

        throw new GlslSyntaxException($"Unterminated '{opening}'", start.Location);
    }

    private static bool TryGetDeclarationKind(Token token, out DeclarationKind kind)
    {
        kind = token.Value switch
        {
            "uniform" => DeclarationKind.Uniform,
            "buffer" => DeclarationKind.Buffer,
            "in" => DeclarationKind.Input,
            "out" => DeclarationKind.Output,
            _ => default,
        };

        return token.Type == TokenType.Keyword && token.Value is "uniform" or "buffer" or "in" or "out";
    }

    private static Token Require(
        IReadOnlyList<Token> tokens,
        ref int index,
        IReadOnlyList<TokenType> expectedTypes,
        string message)
    {
        if (index >= tokens.Count || !expectedTypes.Contains(tokens[index].Type))
        {
            throw SyntaxError(tokens, index, message);
        }

        return tokens[index++];
    }

    private static void RequireValue(
        IReadOnlyList<Token> tokens,
        ref int index,
        TokenType expectedType,
        string expectedValue,
        string message)
    {
        if (!Is(tokens, index, expectedType, expectedValue))
        {
            throw SyntaxError(tokens, index, message);
        }

        index++;
    }

    private static bool Is(IReadOnlyList<Token> tokens, int index, TokenType type, string value)
    {
        return index < tokens.Count && tokens[index].Type == type && tokens[index].Value == value;
    }

    private static GlslSyntaxException SyntaxError(IReadOnlyList<Token> tokens, int index, string message)
    {
        var location = index < tokens.Count
            ? tokens[index].Location
            : tokens.Count > 0
                ? tokens[^1].Location
                : new SourceLocation(0, 1, 1);
        return new GlslSyntaxException(message, location);
    }
}
