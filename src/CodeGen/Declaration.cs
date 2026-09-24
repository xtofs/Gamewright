namespace Gamewright.CodeGen;

public enum DeclarationKind
{
    Uniform,
    Buffer,
    Input,
    Output,
}

public readonly record struct GlslTypeName(string Value);

public readonly record struct ArraySpecifier(int? Length)
{
    public bool IsUnsized => Length is null;
}

public abstract record Declaration(
    DeclarationKind Kind,
    IReadOnlyList<LayoutQualifier> Layout,
    SourceLocation Location);

public sealed record VariableDeclaration(
    DeclarationKind Kind,
    GlslTypeName Type,
    string Name,
    IReadOnlyList<LayoutQualifier> Layout,
    ArraySpecifier? Array,
    SourceLocation Location)
    : Declaration(Kind, Layout, Location);

public sealed record BlockMember(
    GlslTypeName Type,
    string Name,
    ArraySpecifier? Array,
    SourceLocation Location);

public sealed record InterfaceBlockDeclaration(
    DeclarationKind Kind,
    string BlockName,
    IReadOnlyList<BlockMember> Members,
    string? InstanceName,
    IReadOnlyList<LayoutQualifier> Layout,
    ArraySpecifier? InstanceArray,
    SourceLocation Location)
    : Declaration(Kind, Layout, Location);

public sealed class GlslSyntaxException(string message, SourceLocation location)
    : Exception($"{message} at line {location.Line}, column {location.Column}.")
{
    public SourceLocation Location { get; } = location;
}
