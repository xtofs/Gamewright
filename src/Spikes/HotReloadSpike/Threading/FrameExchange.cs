namespace HotReloadSpike.Threading;

using HotReloadSpike.Rendering;

/// <summary>
/// Triple buffer that hands finished command buffers from the game thread to the render thread.
/// The game thread always owns <c>back</c>, the render thread always owns <c>front</c>, and
/// <c>pending</c> is only swapped under the lock, so neither side waits for the other.
/// </summary>
public sealed class FrameExchange
{
    private readonly Lock _lock = new();
    private RenderCommandBuffer _back;
    private RenderCommandBuffer _pending;
    private RenderCommandBuffer _front;
    private bool _hasPending;

    public FrameExchange(int capacity = 16 * 1024)
    {
        _back = new RenderCommandBuffer(capacity);
        _pending = new RenderCommandBuffer(capacity);
        _front = new RenderCommandBuffer(capacity);
    }

    /// <summary>Game thread: returns the cleared back buffer to record the next frame into.</summary>
    public RenderCommandBuffer BeginWrite()
    {
        _back.Clear();
        return _back;
    }

    /// <summary>Game thread: makes the back buffer the latest finished frame.</summary>
    public void Publish()
    {
        lock (_lock)
        {
            (_back, _pending) = (_pending, _back);
            _hasPending = true;
        }
    }

    /// <summary>
    /// Render thread: returns the latest finished frame, or the previous one again if the game
    /// thread hasn't published since.
    /// </summary>
    public RenderCommandBuffer AcquireLatest()
    {
        lock (_lock)
        {
            if (_hasPending)
            {
                (_front, _pending) = (_pending, _front);
                _hasPending = false;
            }

            return _front;
        }
    }
}
