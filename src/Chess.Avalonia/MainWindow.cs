using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gamewright.Avalonia;

namespace Chess.Avalonia;

public class MainWindow : Window
{
    public MainWindow()
    {
        var app = new ChessApp();

        Title = "Chess";
        Width = app.FrameWidth;
        Height = app.FrameHeight;
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x15, 0x22));

        // Images are the same files the browser host serves, linked in as Avalonia resources.
        Game = new GameControl(app, path =>
        {
            using var stream = AssetLoader.Open(new Uri($"avares://Chess.Avalonia/{path}"));
            return new Bitmap(stream);
        });
        Content = Game;
    }

    public GameControl Game { get; }
}
