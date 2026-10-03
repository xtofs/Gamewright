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

    private readonly CardFan _fan;

    private Font _font = default!;
    private TextureAtlas<Card> _sprites = default!;


    public Scene(Window window)
    {
        _window = window;
        var rng = new Random(0);

        var deck = Dandan.CreateDeck();
        deck.Shuffle(rng);

        _hand = deck.Draw(7);
        _fan = new CardFan(_hand.Count);
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
        if (button == MouseButton.Left) { _fan.Focus += 1; }
        else if (button == MouseButton.Right) { _fan.Focus -= 1; }
    }

    public void Render(Canvas canvas, float deltaTime)
    {
        _root.Update(canvas.FramebufferSize);

        for (var i = 0; i < _hand.Count; i++)
        {
            // draw in order of z-index
            var index = _fan.Index[i];
            var card = _hand[index];
            var angle = GetCardAngle(index);
            var rect = GetCardRect(canvas, index, angle);
            canvas.DrawSprite(_sprites, card, rect, rotationRadians: angle);
        }
    }

    private float GetCardAngle(int i)
    {
        return (i - (_hand.Count - 1) / 2f) * 0.4f;
    }

    private Rect GetCardRect(Canvas canvas, int i, float angle)
    {
        // get bottom center for the fan's center
        var center = new Vector2(_root.Rect.Left + _root.Rect.Width / 2f, _root.Rect.Bottom);
        // get the angle for the card in the fan
        var radius = 500; // distance from the center
        var offset = new Vector2(MathF.Sin(angle) * radius, -MathF.Cos(angle) * radius);

        var position = center + offset;
        var rect = new Rect(position.X - 63 * 3, position.Y - 88 * 3, 63 * 6, 88 * 6); // adjust for card size and scale    
        return rect;
    }



}
