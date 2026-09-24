namespace Amazons;

using Gamewright;
using Silk.NET.Input;
using Gamewright.Utilities;
using System.Numerics;

public sealed class Program : IDisposable
{

    public static void Main()
    {
        using var program = new Program();
        program.Run();
    }

    private readonly Window _window;

    private TextureAtlas<Piece> _pieces = default!;

    private Font _font = default!;

    private readonly BoardLayout _boardLayout = new();

    private readonly Occupancy[,] _board;

    private (int File, int Rank)? _selectedSquare;


    public Program()
    {
        _window = new Window("Game of Amazons") { Background = new Color(0x202020) };
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Unload += OnUnload;
        _window.KeyDown += OnKeyDown;
        _window.MouseDown += OnMouseDown;

        _board = SetupBoard();
    }

    private static Occupancy[,] SetupBoard()
    {
        var board = new Occupancy[BoardLayout.N, BoardLayout.N];
        var rng = new Random(0);
        foreach (var (f, r) in Enumerable.Cartesian(BoardLayout.N, BoardLayout.N))
        {
            if (rng.NextDouble() < 0.5)
            {
                var piece = rng.NextEnum<Piece>();
                board[f, r] = piece.ToOccupancy();
            }
        }
        return board;
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
        _pieces = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    private void OnUnload()
    {
        _font?.Dispose();
        _pieces?.Dispose();
    }

    private void OnMouseDown(System.Numerics.Vector2 pos, MouseButton _)
    {
        if (_boardLayout.TryGetSquare(pos, out var square))
        {
            Console.WriteLine("file {0} rank {1}", square.File, square.Rank);
            _selectedSquare = square;
        }
    }

    private void OnRender(Canvas canvas, float deltaTime)
    {
        _boardLayout.Update(canvas.FramebufferSize);

        #region Draw Labels
        var labels = _boardLayout.GetLabels().ToList();
        var labelScale = labels
            .Where(l => !string.IsNullOrEmpty(l.Text))
            .Min(l => canvas.MeasureFitScale(_font, l.Text, l.Rect));
        foreach (var (rect, text, _) in labels)
        {
            canvas.DrawCenteredText(_font, text, rect, labelScale * 0.9f, Colors.White);
        }
        #endregion

        #region Draw Board
        foreach (var (f, r) in Enumerable.Cartesian(BoardLayout.N, BoardLayout.N))
        {
            var (rect, color) = _boardLayout.GetSquare(f, r);
            canvas.DrawRectangle(rect, color);
            var occupancy = _board[f, r];
            if (occupancy.TryGetPiece(out var piece))
            {
                canvas.DrawSprite(_pieces, piece, rect.Inset(5));
            }
        }
        #endregion

        #region Draw Selected Square
        if (_selectedSquare.HasValue)
        {
            var (file, rank) = _selectedSquare.Value;
            var (rect, _) = _boardLayout.GetSquare(file, rank);
            canvas.DrawRoundedRectangle(rect.Inset(5), new CornerRadii(10), Colors.Transparent, new Stroke(Colors.Red, 10));
        }
        #endregion

        if (_selectedSquare.HasValue)
        {
            var a = _boardLayout.GetCenterOfSquare(2, 3);
            var b = _boardLayout.GetCenterOfSquare(_selectedSquare.Value);

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
