namespace Chess;

using System.Numerics;
using Gamewright;

/// <summary>
/// Maps an 8x8 board of ranks and files onto the inner 8×8 of a 10×10 grid, with thin
/// border cells on each side for rank/file labels. All coordinates are in framebuffer pixels.
/// </summary>
public sealed class BoardLayout
{
    private readonly RootNode _root = new();
    private readonly Grid _grid;

    // Border tracks are 1 unit wide; inner tracks are 4 units — so a border is 1/4 of a square size.
    private static readonly GridLength Border = new(1, GridLengthUnit.Proportional);
    private static readonly GridLength Square = new(4, GridLengthUnit.Proportional);

    public BoardLayout()
    {
        var canvas = _root.AddCenteredCanvas(ratio: 1f, insetFraction: 0.01f);

        var colDefs = BuildColumnTracks(Border, Square, 8, Border);
        var rowDefs = BuildRowTracks(Border, Square, 8, Border);
        _grid = canvas.AddGrid(colDefs, rowDefs);
    }

    /// <summary>Recomputes the board layout for the current framebuffer size.</summary>
    public void Update(Vector2 framebufferSize) => _root.Update(framebufferSize);

    /// <summary>Gets the rectangle of the checker square at (file, rank) in framebuffer pixels.</summary>
    public (Rect, Color) GetSquare(int file, int rank)
    {
        if (file < 0 || file >= 8 || rank < 0 || rank >= 8)
        {
            throw new ArgumentOutOfRangeException($"File and rank must be between 0 and 7. Got file={file}, rank={rank}");
        }
        var parity = (file + rank) % 2;
        var color = parity == 0 ? _lightSquare : _darkSquare;
        // Inner squares occupy grid columns/rows 1..8; border is at 0 and 9.
        return (_grid[file + 1, rank + 1].Rect, color);
    }

    private readonly Color _lightSquare = new Color(0xff9e9e9e);
    private readonly Color _darkSquare = new Color(0xff515151);

    /// <summary>
    /// Enumerates all border label cells with their rect, text.
    /// Covers file labels (top + bottom), rank labels (left + right) and corners.
    /// </summary>
    public IEnumerable<(Rect Rect, string Text, Color Color)> GetLabels()
    {
        for (var i = 0; i < 8; i++)
        {
            var fileText = ((char)('a' + i)).ToString();
            var rankText = ((char)('1' + i)).ToString();


            yield return (_grid[i + 1, 0].Rect, fileText, i % 2 == 1 ? _lightSquare : _darkSquare); // top
            yield return (_grid[i + 1, 9].Rect, fileText, i % 2 == 0 ? _lightSquare : _darkSquare); // bottom
            yield return (_grid[0, i + 1].Rect, rankText, i % 2 == 1 ? _lightSquare : _darkSquare); // left
            yield return (_grid[9, i + 1].Rect, rankText, i % 2 == 0 ? _lightSquare : _darkSquare); // right
        }

        yield return (_grid[0, 0].Rect, "", _lightSquare);
        yield return (_grid[9, 0].Rect, "", _darkSquare);
        yield return (_grid[0, 9].Rect, "", _darkSquare);
        yield return (_grid[9, 9].Rect, "", _lightSquare);
    }

    /// <summary>
    /// Converts a framebuffer pixel position to a (file, rank) square.
    /// Returns false if the position falls outside the inner 8×8 board area.
    /// </summary>
    public bool TryGetSquare(Vector2 framebufferPosition, out (int File, int Rank) square)
    {
        if (_root.TryHitTest(framebufferPosition, out var node) && node is Grid.Cell cell
            && cell.Col >= 1 && cell.Col <= 8 && cell.Row >= 1 && cell.Row <= 8)
        {
            square = (cell.Col - 1, cell.Row - 1);
            return true;
        }
        square = default;
        return false;
    }

    private static ColumnDefinition[] BuildColumnTracks(GridLength leading, GridLength inner, int count, GridLength trailing)
    {
        var defs = new ColumnDefinition[count + 2];
        defs[0] = new ColumnDefinition(leading);
        for (var i = 0; i < count; i++)
        {
            defs[i + 1] = new ColumnDefinition(inner);
        }

        defs[count + 1] = new ColumnDefinition(trailing);
        return defs;
    }

    private static RowDefinition[] BuildRowTracks(GridLength leading, GridLength inner, int count, GridLength trailing)
    {
        var defs = new RowDefinition[count + 2];
        defs[0] = new RowDefinition(leading);
        for (var i = 0; i < count; i++)
        {
            defs[i + 1] = new RowDefinition(inner);
        }

        defs[count + 1] = new RowDefinition(trailing);
        return defs;
    }
}
