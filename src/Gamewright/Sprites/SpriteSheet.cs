namespace Gamewright.Sprites;

/// <summary>An image cut into a grid of equally sized cells; <c>sheet[column, row]</c> is the sprite of one cell.</summary>
public sealed class SpriteSheet
{
    public SpriteSheet(Image image, int cellWidth, int cellHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellHeight);
        Image = image;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
    }

    /// <summary>A sheet of square cells.</summary>
    public SpriteSheet(Image image, int cellSize) : this(image, cellSize, cellSize)
    {
    }

    public Image Image { get; }

    public int CellWidth { get; }

    public int CellHeight { get; }

    public Sprite this[int column, int row]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            return new Sprite(Image, new Rect(column * CellWidth, row * CellHeight, CellWidth, CellHeight));
        }
    }
}
