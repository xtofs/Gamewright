namespace Havanna;

using Gamewright.Graphics;
using Gamewright.HexBoard;
using Gamewright.HexBoard.Graphics;
using Silk.NET.Input;
using System.Numerics;

public sealed class HavannaScene : IScene
{
    private const int N = 3;

    #region Window and Graphics Resources initialized in Load
    private readonly Window _window;
    private TextureAtlas<Piece> _spriteTextures = default!;
    private Font _font = default!;
    #endregion

    #region Game state

    // game state    
    private readonly HexGrid<Piece> _board;

    private readonly HexRegionTracker<Piece> _regionTracker;
    #endregion

    #region UI Nodes, Layout and UI State
    private readonly RootNode _root = new();

    private readonly HexBoardNode _boardNode;

    private HexLayout Layout => _boardNode.Layout;

    private CubeCoord? _selectedHex = null;

    private Piece _currentPlayer = Piece.Red;

    // cell indices of the connections that won, turned into screen positions each frame so they follow resizes
    private List<(Color, IReadOnlyList<int>)> _winningPaths = [];
    #endregion

    public HavannaScene(Window window)
    {
        _window = window;
        _boardNode = _root.AddHexBoard(N);

        _board = new HexGrid<Piece>(N);
        _regionTracker = new HexRegionTracker<Piece>(_board);
    }

    public void KeyDown(Key key)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

    public void MouseDown(Vector2 pos, MouseButton _)
    {
        if (_boardNode.TryGetHex(pos, out var hex))
        {
            Console.WriteLine($"Selected hex: {hex.Q},{hex.R},{hex.S}");

            if (!_board.IsOccupied(hex))
            {
                _board.Place(hex, _currentPlayer);
                // _currentPlayer = _currentPlayer.Opponent;

                var index = hex.GetIndex();
                if (_regionTracker.HasBridge(index))
                {
                    Console.WriteLine($"Bridge formed at index: {index}");
                    _winningPaths = [
                        .. _winningPaths,
                        (Colors.GreenYellow, _regionTracker.FindBridgePath(index))
                    ];
                }
                if (_regionTracker.HasFork(index))
                {
                    Console.WriteLine($"Fork formed at index: {index}");
                    _winningPaths = [
                        .. _winningPaths,
                        .. from path in _regionTracker.FindForkPaths(index) select (Colors.IndianRed, path)
                    ];
                }
            }
        }
    }

    const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    public void Load(GraphicsDevice device)
    {
        _spriteTextures = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    public void Unload()
    {
        _font?.Dispose();
        _spriteTextures?.Dispose();
    }
    public void Render(Canvas canvas, float deltaTime)
    {
        _root.Update(canvas.FramebufferSize);

        DrawGrid(canvas);

        DrawPieces(canvas);

        DrawSelection(canvas);

        foreach (var path in _winningPaths)
        {
            var centers = path.Item2.Select(index => Layout.GetCenter(CubeCoord.FromIndex(index))).ToArray();
            canvas.DrawSpline(centers, Layout.HexagonRadius * 0.095f, path.Item1);
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

        void DrawGrid(Canvas canvas)
        {
            foreach (var hex in _board.Coords)
            {
                var corners = Layout.GetHexCorners(hex);

                var fill = GetFillColor(hex);
                var strokeColor = GetStrokeColor(hex);
                var isSelected = _selectedHex == hex;
                var stroke = isSelected
                    ? new Stroke(Colors.HotPink, Layout.HexagonRadius * 0.07f)
                    : new Stroke(strokeColor, Layout.HexagonRadius * 0.025f);

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
                    // canvas.DrawRectangle(rect, stroke: new Stroke(Colors.Black, canvas.PixelsPerPoint));

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
                canvas.DrawPolygon(corners, stroke: new Stroke(Colors.LimeGreen, Layout.HexagonRadius * 0.19f));
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
