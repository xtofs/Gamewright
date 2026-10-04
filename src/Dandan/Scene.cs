namespace Dandan;

using System.Numerics;
using System.Text.Json;
using Gamewright.Graphics;
using Silk.NET.Input;

public sealed class Scene : IScene
{
    private readonly Window _window;

    private readonly RootNode _root = new();
    private readonly IList<Card> _hand;
    private int _focus;
    private readonly FanLayout _layout;
    private readonly CardHitTester _tester;


    private Font _font = default!;
    private TextureAtlas<Card> _sprites = default!;


    public Scene(Window window)
    {
        _window = window;

        var rng = new Random(0);
        var deck = Dandan.CreateDeck();
        deck.Shuffle(rng);
        _hand = deck.Draw(7);
        _focus = _hand.Count / 2;

        // var bottomCenter = new Vector2(window.FramebufferSize.X / 2f, window.FramebufferSize.Y  );
        _layout = new FanLayout { Pivot = new(800, 1200), CardSize = new(600, 800) };
        _tester = new CardHitTester(_layout);
    }

    const string FontPath = "/System/Library/Fonts/Supplemental/Arial.ttf";

    public void Load(GraphicsDevice device)
    {
        _font = device.LoadFont(FontPath, 48);

        var baseDir = AppContext.BaseDirectory;
        var jsonFileName = Path.Combine(baseDir, "resources/dandan-spritesheet.json");
        var pngFileName = Path.Combine(baseDir, "resources/dandan-spritesheet.png");

        var data = JsonSerializer.Deserialize<Root>(File.ReadAllText(jsonFileName));
        var regions = data!.frames.ToDictionary(kvp => CardFromName(kvp.Key), kvp => new Rect(kvp.Value.x, kvp.Value.y, kvp.Value.w, kvp.Value.h));

        _sprites = device.LoadAtlas(pngFileName, regions);


        static Card CardFromName(string name)
        {
            name = name.TrimStart([.. "0123456789-"]);
            name = name.ToPascalCase();

            return Enum.Parse<Card>(name);
        }
    }

    public void MouseDown(Vector2 pos, MouseButton button)
    {
        var order = CardInFocusOrder(_hand.Count, _focus);
        if (_tester.TryHitTest(order, pos, out var slot) && slot != _focus)
        {
            _focus = slot;
            Console.WriteLine($"focus is now {_focus}");
        }
    }

    public void Render(Canvas canvas, float deltaTime)
    {
        _root.Update(canvas.FramebufferSize);

        var order = CardInFocusOrder(_hand.Count, _focus);
        for (var i = 0; i < _hand.Count; i++)
        {
            var card = _hand[order[i]];
            var (center, rotation) = _layout.GetPlacement(order[i], _hand.Count);

            var rect = Rect.FromCenter(center, _layout.CardSize);
            canvas.DrawSprite(_sprites, card, rect, rotationRadians: rotation);
        }
    }

    /// <summary>Returns layout slots from farthest to nearest so the focused card is drawn last.</summary>
    public static int[] CardInFocusOrder(int count, int focus) =>
        [.. Enumerable.Range(0, count).OrderByDescending(i => Math.Abs(i - focus))];
}
