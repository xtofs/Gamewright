namespace Gamewright.Graphics.Utilities;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

public static class EnumCache<T> where T : struct, Enum
{
    public static T[] Values { get; } = Enum.GetValues<T>();

    private static readonly FrozenDictionary<string, T> NameToValueMap =
        Enum.GetValues<T>().ToDictionary(v => v.ToString()).ToFrozenDictionary();

    private static readonly FrozenDictionary<T, string> ValueToNameMap =
        Enum.GetValues<T>().ToDictionary(v => v, v => v.ToString()).ToFrozenDictionary();

    public static bool TryGetValue(string name, [MaybeNullWhen(false)] out T value) =>
        NameToValueMap.TryGetValue(name, out value);

    public static bool TryGetName(T value, [MaybeNullWhen(false)] out string name) =>
        ValueToNameMap.TryGetValue(value, out name);
}
