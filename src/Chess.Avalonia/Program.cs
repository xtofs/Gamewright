using Chess;
using Gamewright.Avalonia;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args) => GameApplication.Run(new ChessApp(), "Chess", args);
}
