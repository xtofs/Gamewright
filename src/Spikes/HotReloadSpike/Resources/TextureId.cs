namespace HotReloadSpike.Resources;

/// <summary>
/// Refers to a texture across threads. The game thread gets it from <see cref="ResourceLoader"/>
/// and puts it into commands; the render thread maps it to a GL texture in <c>TextureStore</c>.
/// It's unmanaged, so it fits into a command struct.
/// </summary>
public readonly record struct TextureId(int Value);
