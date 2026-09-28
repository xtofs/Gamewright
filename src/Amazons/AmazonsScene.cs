namespace Amazons;

using Gamewright.Graphics;
using Gamewright.SquareBoard;
using Gamewright.SquareBoard.Graphics;
using Silk.NET.Input;
using Gamewright.Graphics.Utilities;

public sealed class AmazonsScene : IScene
{

    private const int N = 10;

    private readonly Window _window;

    private TextureAtlas<Piece> _pieces = default!;

    private Font _font = default!;

    private readonly RootNode _root = new();

    private readonly SquareBoardNode _boardNode;

    private readonly SquareGrid<Piece> _board;

    private SquareCoord? _selectedSquare;


    public AmazonsScene(Window window)
    {
        _window = window;
        _boardNode = _root.AddSquareBoard(N, N, insetFraction: 0.01f);
        _board = SetupBoard();
    }

    private static SquareGrid<Piece> SetupBoard()
    {
        var board = new SquareGrid<Piece>(N, N);
        // https://en.wikipedia.org/wiki/Game_of_the_Amazons
        // Place the initial white queens
        board.Place(new SquareCoord(0, 3), Piece.WhiteQueen);
        board.Place(new SquareCoord(0, 6), Piece.WhiteQueen);
        board.Place(new SquareCoord(3, 0), Piece.WhiteQueen);
        board.Place(new SquareCoord(3, 9), Piece.WhiteQueen);

        // Place the initial black queens
        board.Place(new SquareCoord(6, 0), Piece.BlackQueen);
        board.Place(new SquareCoord(6, 9), Piece.BlackQueen);
        board.Place(new SquareCoord(9, 3), Piece.BlackQueen);
        board.Place(new SquareCoord(9, 6), Piece.BlackQueen);

        return board;
    }

    public void KeyDown(Key key)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

    const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    public void Load(GraphicsDevice device)
    {
        _pieces = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    public void Unload()
    {
        _font?.Dispose();
        _pieces?.Dispose();
    }

    public void MouseDown(System.Numerics.Vector2 pos, MouseButton _)
    {
        if (_boardNode.TryGetSquare(pos, out var square))
        {
            Console.WriteLine("file {0} rank {1}", square.File, square.Rank);
            _selectedSquare = square;
        }
    }

    public void Render(Canvas canvas, float deltaTime)
    {
        _root.Update(canvas.FramebufferSize);

        #region Draw Labels
        var labels = _boardNode.GetLabels().ToList();
        var labelScale = labels
            .Where(l => !string.IsNullOrEmpty(l.Text))
            .Min(l => canvas.MeasureFitScale(_font, l.Text, l.Rect));
        foreach (var (rect, text) in labels)
        {
            canvas.DrawCenteredText(_font, text, rect, labelScale * 0.9f, Colors.White);
        }
        #endregion

        #region Draw Board
        foreach (var square in _board.Coords)
        {
            var (rect, color) = _boardNode.GetSquare(square);
            canvas.DrawRectangle(rect, color);
            if (_board.TryGet(square, out var piece))
            {
                canvas.DrawSprite(_pieces, piece, rect.Inset(5));
            }
        }
        #endregion

        #region Draw Selected Square
        if (_selectedSquare.HasValue)
        {
            var (rect, _) = _boardNode.GetSquare(_selectedSquare.Value);
            canvas.DrawRoundedRectangle(rect.Inset(5), new CornerRadii(10), Colors.Transparent, new Stroke(Colors.Red, 10));
        }
        #endregion

        if (_selectedSquare.HasValue)
        {
            var a = _boardNode.GetCenter(new SquareCoord(2, 3));
            var b = _boardNode.GetCenter(_selectedSquare.Value);

            canvas.DrawArrow(a, b, 16, Colors.Red.WithAlpha(0.8f));
        }

#if !SHOW_FPS
        var scale = MathF.Min(canvas.FramebufferSize.X, canvas.FramebufferSize.Y);
        canvas.DrawText(_font, $"{1f / _window.FrameTime:f0}",
                new System.Numerics.Vector2(scale * 0.01f, scale * 0.01f), Colors.Black);
#endif
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
    }

    private static Rect GetRegion(Piece piece)
    {
        var (col, row) = piece switch
        {
            Piece.WhiteQueen => (0, 0),
            Piece.BlackQueen => (0, 1),
            Piece.Flame => (1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, null)
        };
        return new Rect(col * 250, row * 250, 250, 250);
    }
}

public class Game
{

    private const int N = 10;
    private SquareGrid<Piece> _board;
    private Piece _turn;

    Game()
    {
        var board = new SquareGrid<Piece>(N, N);
        // https://en.wikipedia.org/wiki/Game_of_the_Amazons
        // Place the initial white queens
        board.Place(new SquareCoord(0, 3), Piece.WhiteQueen);
        board.Place(new SquareCoord(0, 6), Piece.WhiteQueen);
        board.Place(new SquareCoord(3, 0), Piece.WhiteQueen);
        board.Place(new SquareCoord(3, 9), Piece.WhiteQueen);

        // Place the initial black queens
        board.Place(new SquareCoord(6, 0), Piece.BlackQueen);
        board.Place(new SquareCoord(6, 9), Piece.BlackQueen);
        board.Place(new SquareCoord(9, 3), Piece.BlackQueen);
        board.Place(new SquareCoord(9, 6), Piece.BlackQueen);

        _board = board;
        _turn = Piece.WhiteQueen;
    }

    /// <summary>
    /// This arrow may travel in any orthogonal or diagonal direction 
    /// (even backwards along the same path the amazon just traveled, into or across the starting square if desired). 
    /// An arrow, like an amazon, cannot cross or enter a square where another arrow has landed or an amazon of either color stands. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public IReadOnlyList<SquareCoord> GetArrowTargetsIfQueenMovedTo(SquareCoord from, SquareCoord to)
    {

        if (_board.TryGet(from, out var piece) && piece == _turn && !_board.TryGet(to, out var _))
        {
            var targets = new List<SquareCoord>();
            // foreach (var direction in SquareDirections.OrthogonalAndDiagonal)
            // {
            //     for (var d = 0; ; d += 1)
            //     {
            //         var coord = to.Offset(d, direction);
            //         if (!_board.Contains(coord)) { break; }
            //         if (_board.TryGet(coord, out var _)) { break; }

            //         targets.Add(coord);                    
            //     }
            // }
            return targets;
        }
        return [];
    }
}
