namespace Dandan;

static class EnumerableExtensions
{
    public static IEnumerable<T> Repeat<T>(this KeyValuePair<T, int> kvp) => Enumerable.Repeat(kvp.Key, kvp.Value);
    public static IEnumerable<T> Repeat<T>(this (T Key, int Value) kvp) => Enumerable.Repeat(kvp.Key, kvp.Value);
}
