namespace Gamewright.CodeGen;

public enum BufferLayoutStandard
{
    Std140,
    Std430,
}

public sealed record GlslTypeLayout(
    int Alignment,
    int Size,
    int? ArrayStride = null,
    int? MatrixStride = null);

public sealed record BlockMemberLayout(BlockMember Member, int Offset, GlslTypeLayout TypeLayout);

public sealed record InterfaceBlockLayout(
    BufferLayoutStandard Standard,
    int Alignment,
    int Size,
    IReadOnlyList<BlockMemberLayout> Members);

public static class BufferLayoutCalculator
{
    public static InterfaceBlockLayout Calculate(InterfaceBlockDeclaration block)
    {
        ArgumentNullException.ThrowIfNull(block);
        var standard = GetStandard(block);
        var members = new List<BlockMemberLayout>(block.Members.Count);
        var offset = 0;
        var maximumAlignment = 0;

        foreach (var member in block.Members)
        {
            var typeLayout = GetTypeLayout(member.Type, member.Array, standard, member.Location);
            offset = RoundUp(offset, typeLayout.Alignment);
            members.Add(new BlockMemberLayout(member, offset, typeLayout));
            maximumAlignment = Math.Max(maximumAlignment, typeLayout.Alignment);
            offset += typeLayout.Size;
        }

        var blockAlignment = standard == BufferLayoutStandard.Std140
            ? RoundUp(maximumAlignment, 16)
            : maximumAlignment;
        var size = RoundUp(offset, blockAlignment);
        return new InterfaceBlockLayout(standard, blockAlignment, size, members);
    }

    private static BufferLayoutStandard GetStandard(InterfaceBlockDeclaration block)
    {
        var hasStd140 = block.Layout.Any(qualifier => qualifier is { Name: "std140", Value: null });
        var hasStd430 = block.Layout.Any(qualifier => qualifier is { Name: "std430", Value: null });
        if (hasStd140 == hasStd430)
        {
            throw new GlslSyntaxException(
                "An interface block must declare exactly one of std140 or std430",
                block.Location);
        }

        if (block.Kind == DeclarationKind.Uniform && hasStd430)
        {
            throw new GlslSyntaxException("Uniform blocks cannot use std430", block.Location);
        }

        return hasStd140 ? BufferLayoutStandard.Std140 : BufferLayoutStandard.Std430;
    }

    private static GlslTypeLayout GetTypeLayout(
        GlslTypeName type,
        ArraySpecifier? array,
        BufferLayoutStandard standard,
        SourceLocation location)
    {
        var elementLayout = GetElementLayout(type, standard, location);
        if (array is null)
        {
            return elementLayout;
        }

        var alignment = standard == BufferLayoutStandard.Std140
            ? RoundUp(elementLayout.Alignment, 16)
            : elementLayout.Alignment;
        var stride = RoundUp(elementLayout.Size, alignment);
        var size = array.Value.Length is { } length ? stride * length : 0;
        return new GlslTypeLayout(alignment, size, stride, elementLayout.MatrixStride);
    }

    private static GlslTypeLayout GetElementLayout(
        GlslTypeName type,
        BufferLayoutStandard standard,
        SourceLocation location)
    {
        if (TryGetScalarSize(type.Value, out var scalarSize))
        {
            return new GlslTypeLayout(scalarSize, scalarSize);
        }

        if (TryParseVector(type.Value, out scalarSize, out var components))
        {
            var alignment = components == 2 ? scalarSize * 2 : scalarSize * 4;
            return new GlslTypeLayout(alignment, scalarSize * components);
        }

        if (TryParseMatrix(type.Value, out scalarSize, out var columns, out var rows))
        {
            var vectorAlignment = rows == 2 ? scalarSize * 2 : scalarSize * 4;
            var alignment = standard == BufferLayoutStandard.Std140
                ? RoundUp(vectorAlignment, 16)
                : vectorAlignment;
            var vectorSize = scalarSize * rows;
            var stride = RoundUp(vectorSize, alignment);
            return new GlslTypeLayout(alignment, stride * columns, MatrixStride: stride);
        }

        throw new GlslSyntaxException($"Unsupported buffer member type '{type.Value}'", location);
    }

    private static bool TryGetScalarSize(string type, out int size)
    {
        size = type switch
        {
            "bool" or "int" or "uint" or "float" => 4,
            "double" => 8,
            _ => 0,
        };
        return size != 0;
    }

    private static bool TryParseVector(string type, out int scalarSize, out int components)
    {
        scalarSize = type.StartsWith('d') ? 8 : 4;
        var suffix = type[^1];
        components = suffix is >= '2' and <= '4' ? suffix - '0' : 0;
        return components != 0 && type[..^1] is "vec" or "bvec" or "ivec" or "uvec" or "dvec";
    }

    private static bool TryParseMatrix(string type, out int scalarSize, out int columns, out int rows)
    {
        scalarSize = type.StartsWith('d') ? 8 : 4;
        var matrix = scalarSize == 8 ? type[1..] : type;
        columns = 0;
        rows = 0;
        if (!matrix.StartsWith("mat", StringComparison.Ordinal))
        {
            return false;
        }

        var dimensions = matrix[3..].Split('x');
        if (!int.TryParse(dimensions[0], out columns))
        {
            return false;
        }

        rows = dimensions.Length == 1
            ? columns
            : int.TryParse(dimensions[1], out var parsedRows) ? parsedRows : 0;
        return columns is >= 2 and <= 4 && rows is >= 2 and <= 4;
    }

    private static int RoundUp(int value, int alignment)
    {
        return alignment == 0 ? value : (value + alignment - 1) / alignment * alignment;
    }
}
