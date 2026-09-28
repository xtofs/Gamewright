namespace HotReloadSpike.Host;

using System.Numerics;
using HotReloadSpike.Rendering;
using HotReloadSpike.Resources;
using HotReloadSpike.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.Window;

/// <summary>
/// The native side: owns the Silk.NET window on the main thread (GLFW requires that on macOS)
/// and replays the latest published command buffer every frame. It never calls game code.
/// </summary>
public sealed class RenderHost : IDisposable
{
    private readonly IWindow _window;
    private readonly FrameExchange _frames;
    private readonly HostChannel _channel;
    private readonly TextureUploadQueue _uploads;
    private TextureStore? _textures;
    private RenderContext? _context;
    private IInputContext? _input;
    private bool _closed;

    public RenderHost(
        string title, FrameExchange frames, HostChannel channel, TextureUploadQueue uploads,
        int width = 1000, int height = 700)
    {
        _frames = frames;
        _channel = channel;
        _uploads = uploads;
        _window = SilkWindow.Create(WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core,
                                  ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
        });
        _window.Load += HandleLoad;
        _window.Render += HandleRender;
        _window.FramebufferResize += size => _channel.FramebufferSize = new Vector2(size.X, size.Y);
        _window.Closing += HandleClosing;
    }

    /// <summary>Raised on the main thread when the window closes, before GL resources are released.</summary>
    public event Action? Closing;

    public void Run() => _window.Run();

    public void Dispose()
    {
        HandleClosing();
        _window.Dispose();
    }

    private void HandleLoad()
    {
        var gl = GL.GetApi(_window);
        _textures = new TextureStore(gl, _uploads);
        _context = new RenderContext(gl, _textures);
        _channel.FramebufferSize = new Vector2(_window.FramebufferSize.X, _window.FramebufferSize.Y);

        _input = _window.CreateInput();
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += (_, key, _) => _channel.Input.Enqueue(new KeyDownEvent(key));
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseDown += (m, button) =>
                _channel.Input.Enqueue(new MouseDownEvent(WindowToFramebuffer(m.Position), button));
        }
    }

    private void HandleRender(double deltaTime)
    {
        if (_channel.CloseRequested)
        {
            _window.Close();
            return;
        }

        if (_context is null || _textures is null)
        {
            return;
        }

        // before the replay: every texture a published frame refers to was queued before it
        _textures.ProcessUploads();
        var frame = _frames.AcquireLatest();
        _context.BeginFrame(new Vector2(_window.FramebufferSize.X, _window.FramebufferSize.Y));
        RenderCommandExecutor.Execute(frame.Data, _context);
        _context.EndFrame();
    }

    private void HandleClosing()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        Closing?.Invoke();
        _input?.Dispose();
        _context?.Dispose();
        _textures?.Dispose();
    }

    private Vector2 WindowToFramebuffer(Vector2 position)
        => position * new Vector2(_window.FramebufferSize.X, _window.FramebufferSize.Y)
                    / new Vector2(_window.Size.X, _window.Size.Y);
}
