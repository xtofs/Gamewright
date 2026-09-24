namespace Gamewright.Utilities;

public static class RandomExtensions
{
    public static TEnum NextEnum<TEnum>(this Random rng) where TEnum : struct, Enum
    {
        var values = EnumCache<TEnum>.Values;
        return values[rng.Next(values.Length)];
    }
}
