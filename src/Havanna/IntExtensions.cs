namespace Havanna;

internal static class IntExtensions
{
    extension(int)
    {
        public static int Mod(int a, int m)
        {
            var r = a % m;
            return r < 0 ? r + m : r;
        }
    }
}
