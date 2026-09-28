namespace Amazons;

using Gamewright.Graphics;
using Gamewright.SquareBoard;
using Gamewright.SquareBoard.Graphics;
using Silk.NET.Input;
using Gamewright.Graphics.Utilities;

public sealed class AmazonsScene : IScene
{

    private const int N = 10;

    // ////////////////////////
    // Gamewright window and  resources
    private readonly Window _window;

    private Font _font = default!;
    private TextureAtlas<Piece> _textures = default!;


    // ///////////////
    // Layout nodes for the scene
    private readonly RootNode _root = new();

    private readonly SquareBoardNode _boardNode;

    // ////////////////
    // game state

    private readonly Game _game;
    private readonly SelectionState _selection;

    public AmazonsScene(Window window)
    {
        _window = window;
        _boardNode = _root.AddSquareBoard(N, N, insetFraction: 0.01f);

        _game = new Game();
        _selection = new SelectionState();
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
        _textures = CreatePieceAtlas(device);
        _font = device.LoadFont(FontPath, 48);
    }

    public void Unload()
    {
        _font?.Dispose();
        _textures?.Dispose();
    }

    public void MouseDown(System.Numerics.Vector2 pos, MouseButton _)
    {
        if (_boardNode.TryGetSquare(pos, out var square))
        {
            switch (_selection.Phase)
            {
                case Phase.AwaitingSelection:
                    var selectable = _game.CurrentPlayer == Player.Player1 ? Piece.WhiteQueen : Piece.BlackQueen;
                    if (_game.Board.TryGet(square, out var piece) && piece == selectable)
                    {
                        _selection.SelectAmazon(square);
                    }
                    break;
                case Phase.AwaitingMove:
                    if (_selection.MoveAmazon(_game, square))
                    {
                        _game.Board.Place(_selection.AmazonTarget!.Value, _game.CurrentPlayer == Player.Player1 ? Piece.WhiteQueen : Piece.BlackQueen);
                        _game.Board.Remove(_selection.SelectedAmazon!.Value);
                    }
                    break;
                case Phase.AwaitingShot:
                    if (_selection.ShootArrow(_game, square, out var move))
                    {
                        _game.Board.Place(move.Arrow, Piece.Fire);
                    }
                    break;
            }
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
        foreach (var square in _game.Board.Coords)
        {
            var (rect, color) = _boardNode.GetSquareAndColor(square);
            canvas.DrawRectangle(rect, color);
        }
        #endregion

        #region Selection
        if (_selection.SelectedAmazon is { } selected)
        {
            if (_selection.AmazonTarget is { } target)
            {
                var start = _boardNode.GetCenter(selected);
                var end = _boardNode.GetCenter(target);
                canvas.DrawArrow(start, end, _boardNode.GetSquare(selected).Fraction(0.12f), Colors.GreenYellow);

                foreach (var square in _game.GetAvailableArrowTargets(target))
                {
                    canvas.DrawSprite(_textures, Piece.Fire, _boardNode.GetSquare(square), Color.FromARGB(0xFF888888));
                }
            }
            else
            {
                var (rect, _) = _boardNode.GetSquareAndColor(selected);
                canvas.DrawRoundedRectangle(rect.Inset(rect.Fraction(0.03f)), new CornerRadii(rect.Fraction(0.06f)),
                    Colors.Transparent, new Stroke(Colors.GreenYellow, rect.Fraction(0.06f)));
                foreach (var square in _game.GetAvailableArrowTargets(selected))
                {
                    canvas.DrawSprite(_textures, Piece.WhiteQueen, _boardNode.GetSquare(square), Color.FromARGB(0xFF888888));
                }
            }
        }
        #endregion

        #region Draw Pieces
        foreach (var square in _game.Board.Coords)
        {
            var (rect, color) = _boardNode.GetSquareAndColor(square);
            if (_game.Board.TryGet(square, out var piece))
            {
                canvas.DrawSprite(_textures, piece, rect.Inset(5));
            }
        }
        #endregion


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
            Piece.Fire => (1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, null)
        };
        return new Rect(col * 250, row * 250, 250, 250);
    }
}
