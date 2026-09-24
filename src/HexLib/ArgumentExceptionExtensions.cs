namespace HexLib;

public static class ArgumentExceptionExtensions
{
    extension(ArgumentException)
    {
        public static void ThrowIfNotZero(int value, string message)
        {
            if (value != 0)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
