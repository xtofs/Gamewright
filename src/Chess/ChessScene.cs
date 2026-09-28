namespace Chess;

using Gamewright.Graphics;
using Gamewright.SquareBoard;
using Gamewright.SquareBoard.Graphics;
using Silk.NET.Input;
using Gamewright.Graphics.Utilities;

public sealed class ChessScene : IScene
{
    private readonly Window _window;

    private TextureAtlas<Piece> _sprites = default!;

    private Font _font = default!;

    private readonly RootNode _root = new();

    private readonly SquareBoardNode _boardNode;

    private readonly SquareGrid<Piece> _board;

    private SquareCoord? _selectedSquare;

    public ChessScene(Window window)
    {
        _window = window;
        _boardNode = _root.AddSquareBoard(8, 8, insetFraction: 0.01f);
        _board = SetupBoard();
    }

    private static SquareGrid<Piece> SetupBoard()
    {
        var board = new SquareGrid<Piece>(8, 8);
        var rng = new Random(0);
        foreach (var square in board.Coords)
        {
            if (rng.NextDouble() < 0.5)
            {
                board.Place(square, rng.NextEnum<Piece>());
            }
        }
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
        _sprites = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    public void Unload()
    {
        _font?.Dispose();
        _sprites?.Dispose();
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
                canvas.DrawSprite(_sprites, piece, rect.Inset(5));
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

#if SHOW_FPS
        var scale = MathF.Min(canvas.FramebufferSize.X, canvas.FramebufferSize.Y);
        canvas.DrawText(_font, $"{1f / _window.FrameTime:f0}",
            new System.Numerics.Vector2(scale * 0.01f, scale * 0.01f), Colors.Black);
#endif
    }

    private static TextureAtlas<Piece> CreatePieceAtlas(GraphicsDevice device)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pieces_atlas.png");
        if (!File.Exists(path))
            throw new FileNotFoundException("The chess piece atlas was not found.", path);
        var regions = Enum.GetValues<Piece>().ToDictionary(p => p, GetRegion);
        return device.LoadAtlas(path, regions);
    }

    private static Rect GetRegion(Piece piece)
    {
        var (col, row) = (((int)piece & 0x7F) - 1, ((int)piece & 0x80) >> 7);
        return new Rect(col * 250, row * 250, 250, 250);
    }
}
