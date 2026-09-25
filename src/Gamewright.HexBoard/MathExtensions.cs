namespace Gamewright.HexBoard;

static class MathExtensions
{
    extension(Math)
    {
        public static int Max(int a, int b, int c)
        {
            return Math.Max(a, Math.Max(b, c));
        }
    }
}
