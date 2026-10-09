namespace Mills;

using System.Numerics;
using Gamewright.Graphics;

public sealed class MillsBoardNode : LeafNode
{
    private Vector2 _center;

    public float Delta { get; set; }

    protected override Rect Layout(Rect rect)
    {

        _center = rect.Center;
        var radius = MathF.Min(rect.Width, rect.Height) / 2;
        Delta = radius / 3; // 3 "rings" on the board
        return rect;
    }

    private static readonly (int X, int Y)[] offsets = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];

    // Converts a ring and index to a normalized 2D position on the board.
    // normalized relative to the center of the board and scaled by the ring number.
    private static Vector2 NormalizedCellCenter(int ring, int index)
    {
        var (x, y) = offsets[index];
        return new Vector2(x, y) * (ring + 1);
    }

    /// <summary>
    /// get the 2D position of the specified coordinate on the board.
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public Vector2 GetPosition(Coordinate coord)
    {
        return NormalizedCellCenter(coord.Ring, coord.Index) * Delta + _center;
    }

    /// <summary>
    /// Tries to get the board coordinate corresponding to the specified 2D position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="square"></param>
    /// <returns></returns>
    public bool TryGetCell(Vector2 position, out Coordinate square)
    {
        // Convert the position to a coordinate relative to the center and scaled by _delta
        var localPos = (position - _center) / Delta;
        for (var ring = 0; ring < 3; ring++)
        {
            for (var index = 0; index < 8; index++)
            {
                var cellPos = NormalizedCellCenter(ring, index);
                if (Vector2.Distance(localPos, cellPos) < 0.3f)
                {
                    square = new Coordinate(ring, index);
                    return true;
                }
            }
        }
        square = default;
        return false;
    }

    public void Render(Canvas canvas, MillsBoard board, TextureAtlas<Piece> sprites)
    {
        var ringDistance = this.Delta;
        var lineWidth = ringDistance * .04f;
        var circleRadius = ringDistance * .05f;
        var pieceSize = new Vector2(ringDistance * .7f, ringDistance * .7f);

        // lines connecting the mills board positions
        for (var ring = 0; ring < 3; ring++)
        {
            for (var index = 0; index < 8; index++)
            {
                var a = this.GetPosition((ring, index));
                var b = this.GetPosition((ring, (index + 1) % 8));

                canvas.DrawLine(a, b, lineWidth, Colors.Goldenrod);

                if (ring < 2 && index % 2 == 0)
                {
                    var c = this.GetPosition((ring, index));
                    var d = this.GetPosition((ring + 1, index));
                    canvas.DrawLine(c, d, lineWidth, Colors.Goldenrod);
                }
            }
        }

        // circles at the mills board positions
        for (var ring = 0; ring < 3; ring++)
        {
            for (var index = 0; index < 8; index++)
            {
                var vec = this.GetPosition(new Coordinate(ring, index));
                canvas.DrawCircle(vec, circleRadius, Colors.Goldenrod);
            }
        }

        // draw the pieces on the mills board
        for (var ring = 0; ring < 3; ring++)
        {
            for (var index = 0; index < 8; index++)
            {
                var pos = this.GetPosition((ring, index));

                if (board[(ring, index)] is { } piece)
                {
                    var rect = Rect.FromCenter(pos, pieceSize);

                    if (board.IsPartOfMill(new Coordinate(ring, index)))
                    {
                        canvas.DrawCircle(pos, ringDistance / 2, Colors.Yellow);
                    }

                    // Draw the piece at the appropriate location
                    canvas.DrawSprite(sprites, piece, rect);
                }
            }
        }
    }
}
