namespace Havanna;

using Gamewright;
using Gamewright.HexBoard;
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

    private Font _font = default!;

    private const int N = 5;
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

        // _board = SetupBoard();
    }

    // private static Occupancy[,] SetupBoard()
    // {
    //     var board = new Occupancy[8, 8];
    //     var rng = new Random(0);
    //     foreach (var (f, r) in Enumerable.Cartesian(8, 8))
    //     {
    //         if (rng.NextDouble() < 0.5)
    //         {
    //             var piece = rng.NextEnum<Piece>();
    //             board[f, r] = piece.ToOccupancy();
    //         }
    //     }
    //     return board;
    // }

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
        // _sprites = CreatePieceAtlas(device);
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
            _selectedHex = hex;
            Console.WriteLine($"Selected hex: {hex.Q},{hex.R},{hex.S}");
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

            var fill = GetColor(hex);
            var isSelected = _selectedHex == hex;
            var stroke = isSelected ? new Stroke(Colors.HotPink, 9) : new Stroke(Colors.Yellow, 1);
            canvas.DrawPolygon(pts, fill: fill, stroke: stroke);

            var sz = _layout.HexRadius / 2f;
            var rect = new Rect(point.X - sz, point.Y - sz, 2 * sz, 2 * sz);
            canvas.DrawCenteredText(_font, $"{hex.Q},{hex.R},{hex.S}", rect, sz / 60f, Colors.White);
        }

        static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
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
        static Color GetColor(CubeCoord hex)
        {
            return Mod(hex.Q - hex.R, 3) switch
            {
                0 => Color.FromARGB(0xff404040),
                1 => Color.FromARGB(0xff808080),
                2 => Color.FromARGB(0xffB0B0B0),
                _ => throw new InvalidOperationException("Unexpected modulo result")
            };
        }
    }



    // private static TextureAtlas<Piece> CreatePieceAtlas(GraphicsDevice device)
    // {
    //     var path = Path.Combine(AppContext.BaseDirectory, "pieces_atlas.png");
    //     if (!File.Exists(path))
    //         throw new FileNotFoundException("The chess piece atlas was not found.", path);
    //     var regions = Enum.GetValues<Piece>().ToDictionary(p => p, GetRegion);
    //     return device.LoadAtlas(path, regions);

    //     static Rect GetRegion(Piece piece)
    //     {
    //         var (col, row) = (((int)piece & 0x7F) - 1, ((int)piece & 0x80) >> 7);
    //         return new Rect(col * 250, row * 250, 250, 250);
    //     }
    // }

}
