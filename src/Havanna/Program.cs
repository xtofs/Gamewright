namespace Havanna;

using Gamewright.Graphics;

public static class Program
{
    public static void Main()
    {
        using var window = new Window("Havanna") { Background = new Color(0x202020) };
        window.Run(new HavannaScene(window));
    }
}
