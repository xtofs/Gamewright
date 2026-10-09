namespace Mills;

using System.Numerics;
using Gamewright.Graphics;
using Silk.NET.Input;

public sealed class Scene : IScene
{
    private readonly Window _window;

    private TextureAtlas<Piece> _sprites = default!;

    private Font _font = default!;

    private readonly RootNode _root = new();

    readonly MillsBoardNode _boardNode = default!;

    private readonly MillsBoard _board = new MillsBoard();



    public Scene(Window window)
    {
        _window = window;
        _board = new MillsBoard();
        _board[(0, 2)] = Piece.White;

        _boardNode = _root
             .AddCenteredCanvas(1, 0.10f) // 10% border
             .Add(new MillsBoardNode());
    }


    public void KeyDown(Key key)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

    public void MouseDown(Vector2 pos, MouseButton button)
    {
        if (_boardNode.TryGetCell(pos, out var cell))
        {
            var active = _board.ActivePlayer;
            if (_board[cell] is not null)
            {
                _board[cell] = null;
            }
            else
            {
                _board[cell] = active;
            }
        }
    }


    public void Render(Canvas canvas, float deltaTime)
    {
        _root.Update(canvas.FramebufferSize);

        _boardNode.Render(canvas, _board, _sprites);


#if !NO_FPS
        var scale = MathF.Min(canvas.FramebufferSize.X, canvas.FramebufferSize.Y);
        canvas.DrawText(_font, $"{1f / _window.FrameTime:f0}",
                new System.Numerics.Vector2(scale * 0.01f, scale * 0.01f), Colors.Black);
#endif
    }

    private static readonly (int X, int Y)[] offsets = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];

    private static Vector2 GetMillsCellPosition(int ring, int index)
    {
        var (x, y) = offsets[index];
        var o = new Vector2(x, y);
        var pos = o * (ring + 1);
        return pos;
    }

    private static TextureAtlas<Piece> CreatePieceAtlas(GraphicsDevice device)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pieces_atlas.png");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The Mills piece atlas was not found.", path);
        }

        var regions = Enum.GetValues<Piece>().ToDictionary(p => p, GetRegion);
        return device.LoadAtlas(path, regions);

        static Rect GetRegion(Piece piece)
        {
            var (col, row) = (0, (int)piece + 1);
            return new Rect(col * 160, row * 160, 160, 160);
        }
    }


    const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    public void Load(GraphicsDevice device)
    {
        _sprites = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    public void Unload()
    {
        _font?.Dispose();
        _sprites?.Dispose();
    }
}
