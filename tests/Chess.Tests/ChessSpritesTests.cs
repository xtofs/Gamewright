using System.Buffers.Binary;
using Chess;
using Gamewright;
using Gamewright.Sprites;

namespace Chess.Tests;

public class ChessSpritesTests
{
    private static string ImageFile()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gamewright.slnx")))
                return Path.Combine(directory.FullName, "src", "Chess.Browser", "wwwroot", ChessSprites.Image.Path);
        }
        throw new InvalidOperationException("Could not find the repository root.");
    }

    private static (int Width, int Height) ImageSize()
    {
        // PNG: 8-byte signature, then the IHDR chunk (length, type) before width and height.
        var header = new byte[24];
        using (var stream = File.OpenRead(ImageFile()))
            stream.ReadExactly(header);
        return (BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)));
    }

    private static IEnumerable<Piece> AllPieces() =>
        from color in Enum.GetValues<PieceColor>()
        from kind in Enum.GetValues<PieceKind>()
        select new Piece(color, kind);

    [Fact]
    public void EveryPiecesCellLiesInsideTheImage()
    {
        var (width, height) = ImageSize();
        var sheet = new SpriteSheet(ChessSprites.Image, ChessSprites.CellSize);

        foreach (var piece in AllPieces())
        {
            var (column, row) = ChessSprites.Locate(piece);
            var cell = sheet[column, row].Source;
            Assert.True(cell.X + cell.Width <= width && cell.Y + cell.Height <= height, $"{piece} is outside the image.");
        }
    }

    [Fact]
    public void ImageIsAWholeNumberOfCells()
    {
        var (width, height) = ImageSize();

        Assert.Equal(0, width % ChessSprites.CellSize);
        Assert.Equal(0, height % ChessSprites.CellSize);
    }

    [Fact]
    public void PiecesHaveDistinctCells() =>
        Assert.Equal(12, AllPieces().Select(ChessSprites.Locate).Distinct().Count());

    [Fact]
    public void ColumnsFollowPieceKindAndRowsFollowColor()
    {
        Assert.Equal((0, 2), ChessSprites.Locate(new Piece(PieceColor.White, PieceKind.King)));
        Assert.Equal((5, 2), ChessSprites.Locate(new Piece(PieceColor.White, PieceKind.Pawn)));
        Assert.Equal((0, 3), ChessSprites.Locate(new Piece(PieceColor.Black, PieceKind.King)));
        Assert.Equal((5, 3), ChessSprites.Locate(new Piece(PieceColor.Black, PieceKind.Pawn)));
    }

    [Fact]
    public void UnknownValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChessSprites.Locate(new Piece((PieceColor)9, PieceKind.King)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChessSprites.Locate(new Piece(PieceColor.White, (PieceKind)9)));
    }
}
