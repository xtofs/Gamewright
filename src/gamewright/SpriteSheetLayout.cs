namespace Gamewright;

/// <summary>
/// Layout of a sprite sheet image: a grid of square cells. A sprite index counts cells row by
/// row, so index = row * Columns + column. An app declares its layout (see
/// <see cref="GameApp.SpriteSheet"/>) and the host tells the renderer.
/// </summary>
public readonly record struct SpriteSheetLayout
{
    public SpriteSheetLayout(int columns, int rows, int cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);
        Columns = columns;
        Rows = rows;
        CellSize = cellSize;
    }

    public int Columns { get; }

    public int Rows { get; }

    /// <summary>Width and height of one cell, in pixels of the image.</summary>
    public int CellSize { get; }

    /// <summary>Width of the whole image in pixels.</summary>
    public int Width => Columns * CellSize;

    public int Height => Rows * CellSize;

    public int Count => Columns * Rows;

    public int Index(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        return row * Columns + column;
    }
}
