namespace HotReloadSpike.Threading;

using System.Collections.Concurrent;
using System.Numerics;

/// <summary>
/// Everything besides draw commands that crosses between the render thread and the game thread.
/// </summary>
public sealed class HostChannel
{
    private long _framebufferSize;
    private volatile bool _closeRequested;

    /// <summary>Written by the render thread, drained by the game thread.</summary>
    public ConcurrentQueue<InputEvent> Input { get; } = new();

    public Vector2 FramebufferSize
    {
        get
        {
            var packed = Interlocked.Read(ref _framebufferSize);
            return new Vector2((int)(packed >> 32), (int)packed);
        }
        set => Interlocked.Exchange(ref _framebufferSize, ((long)(int)value.X << 32) | (uint)(int)value.Y);
    }

    public bool CloseRequested => _closeRequested;

    /// <summary>Game thread: asks the render host to close the window on its next frame.</summary>
    public void RequestClose() => _closeRequested = true;
}
