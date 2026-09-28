namespace Amazons;

using Gamewright.Graphics;
using Gamewright.SquareBoard;

public static class Program
{
    public static void Main()
    {
        using var window = new Window("Game of Amazons") { Background = new Color(0x202020) };
        window.Run(new AmazonsScene(window));

    }
}
