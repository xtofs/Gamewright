using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Gamewright.Desktop;

/// <summary>
/// Runs a <see cref="GameApp"/> in a desktop window. An app's executable is a single call:
/// <c>GameApplication.Run(new MyApp(), args)</c>. Images are loaded by path from the
/// executable's Avalonia resources (<c>avares://&lt;assembly&gt;/&lt;path&gt;</c>), so an app includes
/// the files its frames name as <c>AvaloniaResource</c> items.
/// </summary>
/// <remarks>
/// Development aid: <c>--snapshot out.png [--click x,y ...]</c> clicks at the given frame coordinates,
/// saves the game as a PNG and exits, so the result can be checked without a person.
/// </remarks>
public sealed class GameApplication : Application
{
    private readonly GameApp app;
    private readonly string resourceAssembly;

    private GameApplication(GameApp app, string resourceAssembly)
    {
        this.app = app;
        this.resourceAssembly = resourceAssembly;
    }

    /// <summary>Starts the app and returns when its window closes.</summary>
    /// <param name="resourceAssembly">The assembly whose resources hold the images; defaults to the executable.</param>
    public static int Run(GameApp app, string[] args, Assembly? resourceAssembly = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(args);

        var assemblyName = (resourceAssembly ?? Assembly.GetEntryAssembly())?.GetName().Name
            ?? throw new InvalidOperationException("There is no entry assembly to load resources from.");

        return AppBuilder
            .Configure(() => new GameApplication(app, assemblyName))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }

    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new GameWindow(app, app.Title, LoadImage);
            desktop.MainWindow = window;

            var args = desktop.Args ?? [];
            if (Array.IndexOf(args, "--snapshot") is >= 0 and var at && at + 1 < args.Length)
                window.Opened += (_, _) => TakeSnapshot(desktop, window, args[at + 1], args);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private Bitmap? LoadImage(string path)
    {
        var uri = new Uri($"avares://{resourceAssembly}/{path}");
        if (!AssetLoader.Exists(uri))
            return null;

        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

    private static void TakeSnapshot(IClassicDesktopStyleApplicationLifetime desktop, GameWindow window, string path, string[] args)
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
