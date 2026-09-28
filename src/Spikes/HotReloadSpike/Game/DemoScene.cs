namespace HotReloadSpike.Game;

using System.Numerics;
using HotReloadSpike.Resources;
using HotReloadSpike.Threading;
using Silk.NET.Input;

/// <summary>
/// Edit this file while the app runs under <c>dotnet watch</c> or the debugger: changes to
/// <see cref="Update"/> and <see cref="Draw"/> show up on the next tick.
/// </summary>
public sealed class DemoScene : IScene
{
    private const int Columns = 8;
    private const int Rows = 4;
    private const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    // cells of Assets/pieces_atlas.png, 250px each
    private static readonly Rect[] PieceRegions =
    [
        new Rect(0, 0, 250, 250),   // white queen
        new Rect(0, 250, 250, 250), // black queen
        new Rect(250, 250, 250, 250), // flame
    ];

    private float _time;
    private Vector2? _lastClick;
    private Font _font = default!;
    private Texture _pieces = default!;
    private ResourceLoader _resources = default!;

    public void Load(ResourceLoader resources)
    {
        // kept so Draw can load more on demand; the loader caches by path
        _resources = resources;
        _font = resources.Font(FontPath, 32);
        _pieces = resources.Texture("Assets/pieces_atlas.png");
    }

    public void Update(float deltaTime, IReadOnlyList<InputEvent> input, HostChannel host)
    {
        _time += deltaTime;
        foreach (var inputEvent in input)
        {
            switch (inputEvent)
            {
                case KeyDownEvent { Key: Key.Escape }:
                    host.RequestClose();
                    break;
                case MouseDownEvent click:
                    _lastClick = click.Position;
                    break;
            }
        }
    }

    public void Draw(Canvas canvas)
    {
        var size = canvas.Size;
        canvas.Clear(new Vector4(0.12f, 0.12f, 0.14f, 1));

        DrawGrid(canvas);

        var title = $"Hot Reload Spike   t = {_time:f1} s";
        var titleY = (size.Y * 0.05f - _font.LineHeight) / 2;
        canvas.DrawText(_font, title, new Vector2(size.X * 0.05f + 6, titleY), new Vector4(1, 1, 1, 0.9f));

        var shapeY = size.Y * 0.78f;
        var unit = MathF.Min(size.X, size.Y);

        var pulse = 1 + 0.2f * MathF.Sin(_time * 8);
        canvas.DrawCircle(
            new Vector2(size.X * 0.2f, shapeY), unit * 0.08f * pulse,
            Hsv(0.55f, 0.6f, 0.9f), stroke: new Vector4(1), strokeWidth: 3);

        var lineCenter = new Vector2(size.X * 0.5f, shapeY);
        var direction = new Vector2(MathF.Cos(_time), MathF.Sin(_time)) * unit * 0.1f;
        canvas.DrawLine(lineCenter - direction, lineCenter + direction, 12, Hsv(0.1f, 0.8f, 1));

        var triangleCenter = new Vector2(size.X * 0.8f, shapeY);
        var r = unit * 0.1f;
        canvas.DrawTriangle(
            triangleCenter + new Vector2(0, -r),
            triangleCenter + new Vector2(r * 0.87f, r * 0.5f),
            triangleCenter + new Vector2(-r * 0.87f, r * 0.5f),
            Hsv(0.9f, 0.7f, 0.95f));

        if (_lastClick is { } click)
        {
            canvas.DrawCircle(click, 20, Vector4.Zero, stroke: new Vector4(1, 0, 0, 1), strokeWidth: 4);
            canvas.DrawText(_font, $"({click.X:f0}, {click.Y:f0})", click + new Vector2(28, -16), new Vector4(1, 0.4f, 0.4f, 1));
        }
    }

    private void DrawGrid(Canvas canvas)
    {
        var size = canvas.Size;
        var margin = size * 0.05f;
        var area = new Vector2(size.X - 2 * margin.X, size.Y * 0.55f);
        var cell = area / new Vector2(Columns, Rows);
        const float Gap = 6;

        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var hue = (column + row) / (float)(Columns + Rows) + _time * 0.05f;
                var topLeft = margin + cell * new Vector2(column, row);
                canvas.DrawRoundedRectangle(
                    topLeft + new Vector2(Gap),
                    cell - new Vector2(2 * Gap),
                    cornerRadius: 10,
                    Hsv(hue, 0.5f, 0.85f),
                    stroke: new Vector4(1, 1, 1, 0.6f),
                    strokeWidth: 2);

                // a piece in most cells, centered and square
                var piece = (row * Columns + column) % 4;
                if (piece < PieceRegions.Length)
                {
                    var side = MathF.Min(cell.X, cell.Y) * 0.8f;
                    canvas.DrawSprite(_pieces, PieceRegions[piece], topLeft + (cell - new Vector2(side)) / 2, new Vector2(side));
                }
            }
        }
    }

    private static Vector4 Hsv(float hue, float saturation, float value)
    {
        hue = (hue % 1 + 1) % 1 * 6;
        var chroma = value * saturation;
        var x = chroma * (1 - MathF.Abs(hue % 2 - 1));
        var (r, g, b) = (int)hue switch
        {
            0 => (chroma, x, 0f),
            1 => (x, chroma, 0f),
            2 => (0f, chroma, x),
            3 => (0f, x, chroma),
            4 => (x, 0f, chroma),
            _ => (chroma, 0f, x),
        };
        var m = value - chroma;
        return new Vector4(r + m, g + m, b + m, 1);
    }
}
