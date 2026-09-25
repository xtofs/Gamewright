namespace Gamewright.HexBoard;


static class IntExtensions
{
    extension(int value)
    {
        public int Abs() => value < 0 ? -value : value;
        public int Sgn() => value < 0 ? -1 : value > 0 ? 1 : 0;
    }
}
static class ShortExtensions
{
    extension(short value)
    {
        public short Abs() => value < 0 ? (short)(-value) : value;
        public short Sgn() => value < 0 ? (short)-1 : value > 0 ? (short)1 : (short)0;
    }
}
