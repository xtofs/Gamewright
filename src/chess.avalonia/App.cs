using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Themes.Fluent;

namespace Chess.Avalonia;

public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;

            // Development aid: `--snapshot out.png [--click x,y ...]` clicks at the given frame
            // coordinates, saves the board as a PNG and exits, so the result can be checked without a person.
            var args = desktop.Args ?? [];
            if (Array.IndexOf(args, "--snapshot") is >= 0 and var at && at + 1 < args.Length)
                window.Opened += (_, _) => TakeSnapshot(desktop, window, args[at + 1], args);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void TakeSnapshot(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window, string path, string[] args)
    {
        for (var i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] != "--click")
                continue;
            var parts = args[i + 1].Split(',');
            window.Game.Click(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        Dispatcher.UIThread.Post(() =>
        {
            var size = window.Game.Bounds.Size;
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
            bitmap.Render(window.Game);
            bitmap.Save(path);
            desktop.Shutdown();
        }, DispatcherPriority.Background);
    }
}
