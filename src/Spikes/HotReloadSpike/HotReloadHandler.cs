using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(HotReloadSpike.HotReloadHandler))]

namespace HotReloadSpike;

/// <summary>
/// Called by the runtime after a Hot Reload update was applied, so it's visible in the
/// console when an edit landed.
/// </summary>
internal static class HotReloadHandler
{
    public static void ClearCache(Type[]? updatedTypes)
    {
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        var names = updatedTypes is null ? "<unknown>" : string.Join(", ", updatedTypes.Select(t => t.Name));
        Console.WriteLine($"hot reload applied: {names}");
    }
}
