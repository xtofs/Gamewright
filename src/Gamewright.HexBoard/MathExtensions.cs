namespace Gamewright.HexBoard;

using System.Runtime.CompilerServices;

static class MathExtensions
{
    extension(Math)
    {
        public static int Max(int a, int b, int c)
        {
            return Math.Max(a, Math.Max(b, c));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ISqrt(int x)
        {
            if (x <= 0)
            {
                return 0;
            }

            // Compute sqrt in double
            var d = Math.Sqrt(x);
            var r = (int)d;

            // Branchless correction: (the ternary operator is compiled into branchless code by the JIT)
            // r = r - 1 if r*r > x
            var r2 = r * r;
            var adjustDown = r2 > x ? 1 : 0;
            r -= adjustDown;

            // r = r + 1 if (r+1)^2 <= x
            var rp1 = r + 1;
            var rp1sq = rp1 * rp1;
            var adjustUp = rp1sq <= x ? 1 : 0;
            r += adjustUp;

            return r;
        }
    }
}
