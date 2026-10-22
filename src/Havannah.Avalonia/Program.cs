using Gamewright.Avalonia;
using Havannah;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args) => GameApplication.Run(new HavannahApp(), "Havannah", args);
}
