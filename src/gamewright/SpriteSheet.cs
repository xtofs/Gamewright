namespace Gamewright;

/// <summary>
/// Layout of the sprite sheet (<c>sprites.png</c>): a grid of square cells. A sprite index counts
/// cells row by row, so index = row * Columns + column. The renderer is told this layout at
/// startup; a test checks it against the image.
/// </summary>
public static class SpriteSheet
{
    public const int Columns = 7;
    public const int Rows = 4;
    public const int CellSize = 200;

    public const int Width = Columns * CellSize;
    public const int Height = Rows * CellSize;

    public static int Index(int row, int column) => row * Columns + column;
}
