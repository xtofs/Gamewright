namespace HotReloadSpike.Game;

using HotReloadSpike.Resources;
using HotReloadSpike.Threading;

/// <summary>
/// Game code. Runs on the game thread and is called again every tick, so Hot Reload edits to
/// these methods take effect on the next tick.
/// </summary>
public interface IScene
{
    /// <summary>
    /// Called once on the game thread before the first <see cref="Update"/>. Load textures and
    /// fonts here. Hot Reload doesn't call it again, so edits here need a restart; the loader
    /// can also be kept and called from <see cref="Draw"/> (it caches by path).
    /// </summary>
    void Load(ResourceLoader resources);

    void Update(float deltaTime, IReadOnlyList<InputEvent> input, HostChannel host);

    void Draw(Canvas canvas);
}
