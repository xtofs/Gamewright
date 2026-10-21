using Avalonia;
using Chess.Avalonia;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
