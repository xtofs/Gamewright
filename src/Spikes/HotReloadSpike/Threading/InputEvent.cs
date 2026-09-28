namespace HotReloadSpike.Threading;

using System.Numerics;
using Silk.NET.Input;

public abstract record InputEvent;

public sealed record KeyDownEvent(Key Key) : InputEvent;

/// <summary>A mouse press; <paramref name="Position"/> is in framebuffer pixels.</summary>
public sealed record MouseDownEvent(Vector2 Position, MouseButton Button) : InputEvent;
