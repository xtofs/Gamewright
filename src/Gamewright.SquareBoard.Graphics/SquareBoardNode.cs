namespace Gamewright.SquareBoard.Graphics;

using System.Numerics;
using Gamewright.Graphics;
using Gamewright.SquareBoard;

/// <summary>
/// A layout tree node that lays out a board of <see cref="Columns"/> × <see cref="Rows"/> squares
/// in its rect, optionally surrounded by thin border cells for file and rank labels.
/// Rank 0 is at the bottom and file 0 on the left; all coordinates are in framebuffer pixels.
/// </summary>
/// <remarks>
/// The squares fill the rect, so add it through <see cref="SquareBoardNodeExtensions.AddSquareBoard"/>
/// to keep them square.
/// </remarks>
public sealed class SquareBoardNode : SingleChildNode
{
    // border tracks are 1 unit wide, squares 4 units, so a border is 1/4 of a square
    private static readonly GridLength Border = new(1, GridLengthUnit.Proportional);
    private static readonly GridLength Square = new(4, GridLengthUnit.Proportional);

    private readonly Grid _grid;
    private readonly int _border;

    public SquareBoardNode(int files, int ranks, bool withLabels = true, Color? light = null, Color? dark = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(files);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ranks);

        Columns = files;
        Rows = ranks;
        Light = light ?? new Color(0xff9e9e9e);
        Dark = dark ?? new Color(0xff515151);
        _border = withLabels ? 1 : 0;

        _grid = this.AddGrid(
            [.. Tracks(files).Select(length => new ColumnDefinition(length))],
            [.. Tracks(ranks).Select(length => new RowDefinition(length))]);
    }

    public int Columns { get; }

    public int Rows { get; }

    public Color Light { get; }

    public Color Dark { get; }

    /// <summary>The width divided by the height of a board, including its labels.</summary>
    public static float AspectRatio(int files, int ranks, bool withLabels = true)
    {
        var border = withLabels ? 2 : 0;
        return (4f * files + border) / (4f * ranks + border);
    }

    /// <summary>The rectangle of <paramref name="square"/> and its checkerboard color, dark on (0, 0).</summary>
    public (Rect Rect, Color Color) GetSquareAndColor(Coordinate square)
    {
        var sq = GetSquare(square);
        var color = (square.Column + square.Row) % 2 == 0 ? Dark : Light;
        return (sq, color);
    }

    public Rect GetSquare(Coordinate square)
    {
        if (!Contains(square))
        {
            throw new ArgumentOutOfRangeException(nameof(square), square, $"Not on a {Columns}×{Rows} board.");
        }
        return _grid[Column(square.Column), Row(square.Row)].Rect;
    }

    public Vector2 GetCenter(Coordinate square) => GetSquareAndColor(square).Rect.Center;

    /// <summary>
    /// The border cells with their file labels (a, b, c, ...) above and below the board and
    /// rank labels (1, 2, 3, ...) left and right of it. Empty if the board has no labels.
    /// </summary>
    public IEnumerable<(Rect Rect, string Text)> GetLabels()
    {
        if (_border == 0)
        {
            yield break;
        }

        for (var file = 0; file < Columns; file++)
        {
            var text = ((char)('a' + file)).ToString();
            yield return (_grid[Column(file), 0].Rect, text);
            yield return (_grid[Column(file), Rows + 1].Rect, text);
        }

        for (var rank = 0; rank < Rows; rank++)
        {
            var text = (rank + 1).ToString();
            yield return (_grid[0, Row(rank)].Rect, text);
            yield return (_grid[Columns + 1, Row(rank)].Rect, text);
        }
    }

    /// <summary>
    /// Converts a framebuffer position to a square.
    /// Returns false if the position is outside the board, including on the labels.
    /// </summary>
    public bool TryGetSquare(Vector2 position, out Coordinate square)
    {
        // all squares have the same size, so measure from the top left one
        var topLeft = _grid[Column(0), Row(Rows - 1)].Rect;
        if (topLeft.Width <= 0 || topLeft.Height <= 0)
        {
            square = default;
            return false;
        }

        var file = (int)MathF.Floor((position.X - topLeft.X) / topLeft.Width);
        var rowFromTop = (int)MathF.Floor((position.Y - topLeft.Y) / topLeft.Height);
        square = new Coordinate(file, Rows - 1 - rowFromTop);
        return Contains(square);
    }

    protected override Rect Compute(Rect parent) => parent;

    private bool Contains(Coordinate square)
        => square.Column >= 0 && square.Column < Columns && square.Row >= 0 && square.Row < Rows;

    private int Column(int file) => file + _border;

    // grid rows grow downwards, ranks upwards
    private int Row(int rank) => Rows - 1 - rank + _border;

    private IEnumerable<GridLength> Tracks(int count)
    {
        if (_border > 0) { yield return Border; }
        for (var i = 0; i < count; i++) { yield return Square; }
        if (_border > 0) { yield return Border; }
    }
}

public static class SquareBoardNodeExtensions
{
    /// <summary>
    /// Adds a board of <paramref name="files"/> × <paramref name="ranks"/> squares, centered at
    /// its own aspect ratio so the squares stay square.
    /// </summary>
    public static SquareBoardNode AddSquareBoard(
        this Node parent, int files, int ranks, bool withLabels = true, float insetFraction = 0f,
        Color? light = null, Color? dark = null)
        => parent
            .AddCenteredCanvas(SquareBoardNode.AspectRatio(files, ranks, withLabels), insetFraction)
            .Add(new SquareBoardNode(files, ranks, withLabels, light, dark));
}
