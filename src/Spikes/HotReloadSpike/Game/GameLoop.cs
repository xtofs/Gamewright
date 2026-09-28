namespace HotReloadSpike.Game;

using System.Diagnostics;
using System.Numerics;
using HotReloadSpike.Resources;
using HotReloadSpike.Threading;

/// <summary>
/// The managed side: ticks the scene on its own thread at a fixed rate and publishes each
/// recorded frame to the <see cref="FrameExchange"/>.
/// </summary>
public sealed class GameLoop(IScene scene, FrameExchange frames, HostChannel host, ResourceLoader resources)
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1.0 / 60);

    private readonly IScene _scene = scene;
    private readonly FrameExchange _frames = frames;
    private readonly HostChannel _host = host;
    private readonly ResourceLoader _resources = resources;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<InputEvent> _input = [];
    private Thread? _thread;
    private string? _lastError;

    public void Start()
    {
        _thread = new Thread(Run) { Name = "Game", IsBackground = true };
        _thread.Start();
    }

    public void Stop()
    {
        _cancellation.Cancel();
        _thread?.Join();
    }

    // This loop stays on the stack for the app's lifetime, so Hot Reload edits to it only
    // apply after a restart. Everything it calls (Tick, the scene) is re-entered every tick.
    private void Run()
    {
        try
        {
            _scene.Load(_resources);
        }
        catch (Exception ex)
        {
            // keep ticking: the scene may still draw, and the error is visible in the console
            Console.Error.WriteLine($"scene failed to load: {ex}");
        }

        var clock = Stopwatch.StartNew();
        var previous = clock.Elapsed;
        var cancelled = _cancellation.Token.WaitHandle;
        while (!_cancellation.IsCancellationRequested)
        {
            var now = clock.Elapsed;
            Tick((float)(now - previous).TotalSeconds);
            previous = now;

            var remaining = TickInterval - (clock.Elapsed - now);
            if (remaining > TimeSpan.Zero)
            {
                cancelled.WaitOne(remaining);
            }
        }
    }

    private void Tick(float deltaTime)
    {
        // the window isn't open yet, so there's no size to lay out against
        if (_host.FramebufferSize == Vector2.Zero)
        {
            return;
        }

        _input.Clear();
        while (_host.Input.TryDequeue(out var inputEvent))
        {
            _input.Add(inputEvent);
        }

        try
        {
            _scene.Update(deltaTime, _input, _host);
            _scene.Draw(new Canvas(_frames.BeginWrite(), _host.FramebufferSize));
            _frames.Publish();

            if (_lastError is not null)
            {
                Console.WriteLine("scene recovered");
                _lastError = null;
            }
        }
        catch (Exception ex)
        {
            // keep the last good frame on screen; a broken edit shouldn't kill the app
            var error = ex.ToString();
            if (error != _lastError)
            {
                Console.Error.WriteLine($"scene failed, showing last good frame: {error}");
                _lastError = error;
            }
        }
    }
}
