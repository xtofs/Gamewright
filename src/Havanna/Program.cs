namespace Havanna;

using Gamewright.Graphics;
using Gamewright.HexBoard;
using Gamewright.HexBoard.Graphics;
using Silk.NET.Input;
using System.Numerics;

public sealed class Program : IDisposable
{

    public static void Main()
    {
        using var program = new Program();
        program.Run();
    }

    private const int N = 3;

    // UI nodes and graphics resources
    private readonly Window _window;
    private TextureAtlas<Piece> _spriteTextures = default!;
    private Font _font = default!;

    private readonly RootNode _root = new();
    private readonly HexBoardNode _boardNode;
    private HexLayout Layout => _boardNode.Layout;

    // game state    
    private readonly HexGrid<Piece> _board;

    private readonly HexRegionTracker<Piece> _regionTracker;

    private CubeCoord? _selectedHex = null;

    private Piece _currentPlayer = Piece.Red;


    public Program()
    {
        _window = new Window("Havanna") { Background = new Color(0x202020) };
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Unload += OnUnload;
        _window.KeyDown += OnKeyDown;
        _window.MouseDown += OnMouseDown;

        _boardNode = _root.AddHexBoard(N);

        _board = new HexGrid<Piece>(N);
        _regionTracker = new HexRegionTracker<Piece>(_board);

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



    private void OnMouseDown(Vector2 pos, MouseButton _)
    {
        if (_boardNode.TryGetHex(pos, out var hex))
        {
            Console.WriteLine($"Selected hex: {hex.Q},{hex.R},{hex.S}");

            if (!_board.IsOccupied(hex))
            {
                _board.Place(hex, _currentPlayer);
                var index = hex.GetIndex();

#if DEBUG
                Console.WriteLine(_regionTracker.FormatRegion(index));
#endif

                if (_regionTracker.HasBridge(index))
                {
                    Console.WriteLine($"Bridge formed at index: {index}");
                }
                if (_regionTracker.HasFork(index))
                {
                    Console.WriteLine($"Fork formed at index: {index}");
                }

                _currentPlayer = _currentPlayer.Opponent;
            }
        }
        else
        {
        }
    }


    private void OnLoad(GraphicsDevice device)
    {
        _spriteTextures = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    private void OnUnload()
    {
        _font?.Dispose();
        _spriteTextures?.Dispose();
    }

    private void OnRender(Canvas canvas, float deltaTime)
    {

        _root.Update(canvas.FramebufferSize);
        DrawGrid(canvas);

        DrawPieces(canvas);

        DrawSelection(canvas);

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

        void DrawGrid(Canvas canvas)
        {
            foreach (var hex in _board.Coords)
            {
                var corners = Layout.GetHexCorners(hex);

                var fill = GetFillColor(hex);
                var strokeColor = GetStrokeColor(hex);
                var isSelected = _selectedHex == hex;
                var stroke = isSelected ? new Stroke(Colors.HotPink, 9) : new Stroke(strokeColor, 3); ;

                canvas.DrawPolygon(corners, fill: fill, stroke: stroke);
            }
        }

        void DrawPieces(Canvas canvas)
        {
            foreach (var hex in _board.Coords)
            {
                if (_board.TryGet(hex, out var piece))
                {
                    // using the inscribed square for positioning the piece within the hexagon
                    // var (position, size) = Layout.GetInscribedSquare(hex);
                    // var rect = new Rect(position, size);

                    // Use the bounding box of the hexagon’s inscribed circle.
                    var diameter = MathF.Sqrt(3) * Layout.HexagonRadius;
                    var size = new Vector2(diameter);
                    var position = Layout.GetCenter(hex) - size / 2;
                    var rect = new Rect(position, size);

                    // to debug the layout of the piece within the hexagon
                    // canvas.DrawRectangle(rect, stroke: new Stroke(Colors.Black, 1));

                    rect = rect.Inset(0.05f * Layout.HexagonRadius);
                    canvas.DrawSprite(_spriteTextures, piece, rect);
                }
            }
        }

        void DrawSelection(Canvas canvas)
        {
            if (_selectedHex is { } selectedHex)
            {
                var corners = Layout.GetHexCorners(selectedHex);
                canvas.DrawPolygon(corners, stroke: new Stroke(Colors.HotPink, 9));
            }
        }
    }



    private static TextureAtlas<Piece> CreatePieceAtlas(GraphicsDevice device)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pieces_atlas.png");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The chess piece atlas was not found.", path);
        }

        var regions = Enum.GetValues<Piece>().ToDictionary(p => p, GetRegion);
        return device.LoadAtlas(path, regions);

        static Rect GetRegion(Piece piece)
        {
            var (col, row) = piece switch
            {
                Piece.Red => (0, 2),
                Piece.Blue => (0, 3),
                _ => throw new InvalidOperationException("Unexpected piece")
            };

            return new Rect(col * 204 + 24, row * 162, 156, 162);
        }
    }
}
