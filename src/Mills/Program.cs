namespace Mills;

using Gamewright.Graphics;

public static class Program
{
    public static void Main()
    {
        using var window = new Window("Mills") { Background = new Color(0x202020) };
        window.Run(new Scene(window));
    }
}
