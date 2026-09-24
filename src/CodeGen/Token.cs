namespace Gamewright.CodeGen;

public readonly record struct SourceLocation(int Offset, int Line, int Column);

public readonly record struct Token(TokenType Type, string Value, SourceLocation Location);
