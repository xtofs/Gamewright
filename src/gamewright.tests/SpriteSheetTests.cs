using System.Buffers.Binary;
using Gamewright;

namespace Gamewright.Tests;

public class SpriteSheetTests
{
    private static string SpritePath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gamewright.slnx")))
                return Path.Combine(directory.FullName, "src", "browser", "wwwroot", "sprites.png");
        }
        throw new InvalidOperationException("Could not find the repository root.");
    }

    [Fact]
    public void LayoutMatchesTheImage()
    {
        // PNG: 8-byte signature, then the IHDR chunk (length, type) before width and height.
        var header = new byte[24];
        using (var stream = File.OpenRead(SpritePath()))
            stream.ReadExactly(header);

        Assert.Equal(SpriteSheet.Width, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)));
        Assert.Equal(SpriteSheet.Height, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)));
    }

    [Fact]
    public void IndexCountsRowByRow()
    {
        Assert.Equal(0, SpriteSheet.Index(0, 0));
        Assert.Equal(SpriteSheet.Columns, SpriteSheet.Index(1, 0));
        Assert.Equal(SpriteSheet.Columns * SpriteSheet.Rows - 1, SpriteSheet.Index(SpriteSheet.Rows - 1, SpriteSheet.Columns - 1));
    }

    [Fact]
    public void ChessSpritesAreDistinctAndInsideTheSheet()
    {
        var indices = (from color in Enum.GetValues<PieceColor>()
                       from kind in Enum.GetValues<PieceKind>()
                       select ChessSprites.Index(color, kind)).ToArray();

        Assert.Equal(12, indices.Distinct().Count());
        Assert.All(indices, index => Assert.InRange(index, 0, SpriteSheet.Columns * SpriteSheet.Rows - 1));
    }

    [Fact]
    public void ChessSpritesMatchTheSheetArtwork()
    {
        // Row 2 is the white set and row 3 the black set, each starting with the king.
        Assert.Equal(14, ChessSprites.Index(PieceColor.White, PieceKind.King));
        Assert.Equal(19, ChessSprites.Index(PieceColor.White, PieceKind.Pawn));
        Assert.Equal(21, ChessSprites.Index(PieceColor.Black, PieceKind.King));
        Assert.Equal(26, ChessSprites.Index(PieceColor.Black, PieceKind.Pawn));
    }

    [Fact]
    public void ChessSpritesRejectUnknownValues() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ChessSprites.Index((PieceColor)9, PieceKind.King));
}
