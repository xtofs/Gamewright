using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaColor = global::Avalonia.Media.Color;

namespace Gamewright.Avalonia;

/// <summary>A window showing one <see cref="GameApp"/> in a <see cref="GameControl"/>, sized to its frame.</summary>
public sealed class GameWindow : Window
{
    public GameWindow(GameApp app, string title, Func<string, Bitmap?> loadImage)
    {
        ArgumentNullException.ThrowIfNull(app);

        Title = title;
        Width = app.FrameWidth;
        Height = app.FrameHeight;
        Background = new SolidColorBrush(AvaloniaColor.FromRgb(0x10, 0x15, 0x22));
        Game = new GameControl(app, loadImage);
        Content = Game;
    }

    public GameControl Game { get; }
}
