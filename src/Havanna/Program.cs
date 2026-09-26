namespace Havanna;

using Gamewright.Graphics;
using Gamewright.HexBoard;
using Silk.NET.Input;
using Gamewright.Graphics.Utilities;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

public sealed class Program : IDisposable
{

    public static void Main()
    {
        using var program = new Program();
        program.Run();
    }

    private const int N = 8;

    private readonly Window _window;
    private TextureAtlas<Piece> _textures = default!;
    private Font _font = default!;

    private readonly HexGrid<Piece> _board;

    private Piece _currentPlayer = Piece.White;

    private readonly HexLayout _layout = new(N);
    private CubeCoord? _selectedHex;

    public Program()
    {
        _window = new Window("Havanna") { Background = new Color(0x202020) };
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Unload += OnUnload;
        _window.KeyDown += OnKeyDown;
        _window.MouseDown += OnMouseDown;

        _board = new HexGrid<Piece>(N);

        _board.PlacePiece(new CubeCoord(0, 0, 0), Piece.Black);
    }

    private void OnKeyDown(Key key)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

    public void Run() => _window.Run();

    public void Dispose() => _window.Dispose();

    const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    private void OnLoad(GraphicsDevice device)
    {
        _textures = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    private void OnUnload()
    {
        _font?.Dispose();
        // _sprites?.Dispose();
    }

    private void OnMouseDown(Vector2 pos, MouseButton _)
    {
        if (_layout.TryGetHex(pos, out var hex))
        {
            // _selectedHex = hex;
            // Console.WriteLine($"Selected hex: {hex.Q},{hex.R},{hex.S}");

            if (!_board.TryGetContent(hex, out var content))
            {
                _board.PlacePiece(hex, _currentPlayer);
                _currentPlayer = _currentPlayer.Opponent;
            }
        }
        else
        {
            _selectedHex = null;
        }
    }

    private void OnRender(Canvas canvas, float deltaTime)
    {
        #region Draw Board

        _layout.Update(canvas.FramebufferSize);

        foreach (var hex in _layout.GetGrid())
        {
            var point = _layout.GetCenter(hex);
            var pts = _layout.GetHexagon(hex);

            var fill = GetFillColor(hex);
            var strokeColor = GetStrokeColor(hex);
            var isSelected = _selectedHex == hex;
            var stroke = isSelected ? new Stroke(Colors.HotPink, 9) : new Stroke(strokeColor, 3); ;

            canvas.DrawPolygon(pts, fill: fill, stroke: stroke);
        }

        foreach (var hex in _layout.GetGrid())
        {
            if (_board.TryGetContent(hex, out var piece))
            {
                // var point = _layout.GetCenter(hex);

                // var r = _layout.HexRadius;
                // var a = r * (3 - MathF.Sqrt(3));
                // var a2 = a / 2f;

                // var rect = new Rect(point.X - a2, point.Y - a2, a, a);
                var rect = _layout.GetInscribedSquare(hex);
                canvas.DrawSprite(_textures, piece, rect);
                canvas.DrawRectangle(rect, stroke: new Stroke(Colors.Black, 2));
            }
        }

        #endregion

        if (_selectedHex is { } selectedHex)
        {
            var pts = _layout.GetHexagon(selectedHex);
            canvas.DrawPolygon(pts, stroke: new Stroke(Colors.HotPink, 9));
        }
#if !SHOW_FPS
        var scale = MathF.Min(canvas.FramebufferSize.X, canvas.FramebufferSize.Y);
        canvas.DrawText(_font, $"{1f / _window.FrameTime:f0}",
            new System.Numerics.Vector2(scale * 0.01f, scale * 0.01f), Colors.Black);
#endif
        static Color GetFillColor(CubeCoord hex)
        {
            return int.Mod(hex.Q - hex.R, 3) switch
            {
                0 => Color.FromARGB(0xffA0A0A0),
                1 => Color.FromARGB(0xffC0C0C0),
                2 => Color.FromARGB(0xffE0E0E0),
                _ => throw new InvalidOperationException("Unexpected modulo result")
            };
        }

        Color GetStrokeColor(CubeCoord hex)
        {
            return _board.GetKind(hex) switch
            {
                GridLocationKind.Corner => Colors.DarkBlue,
                GridLocationKind.Edge => Colors.DarkSlateBlue,
                _ => Colors.RoyalBlue,
            };
        }
    }



    private static TextureAtlas<Piece> CreatePieceAtlas(GraphicsDevice device)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pieces_atlas.png");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The chess piece atlas was not found.", path);
        }

        var regions = Piece.GetValues().ToDictionary(p => p, GetRegion);
        return device.LoadAtlas(path, regions);

        static Rect GetRegion(Piece piece)
        {
            var (col, row) = piece.Symbol switch
            {
                'X' => (1, 0),
                'O' => (1, 1),
                _ => throw new InvalidOperationException("Unexpected piece")
            };

            return new Rect(col * 250, row * 250, 250, 250);
        }
    }
}
