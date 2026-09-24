namespace Gamewright;

using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Gamewright.Utilities;
using SilkWindow = Silk.NET.Windowing.Window;
using Silk.NET.Windowing;

public sealed class Window : IDisposable
{
    private readonly IWindow _window;
    private Canvas? _canvas;
    private readonly ExponentialMovingAverage _frameTime = new(100);
    private bool _disposed;

    public Window(string title, int width = 1000, int height = 700,
                  int minWidth = 400, int minHeight = 300)
    {
        var minSize = new Vector2D<int>(minWidth, minHeight);
        _window = SilkWindow.Create(WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core,
                                    ContextFlags.ForwardCompatible, new APIVersion(3, 3))
        });
        _window.Load += HandleLoad;
        _window.FramebufferResize += _ => Resize?.Invoke();
        _window.Render += dt => { if (_canvas is { } r) { HandleRender(r, (float)dt); } };
        _window.Closing += HandleClosing;
        _window.Resize += size =>
            _window.Size = size.Clamp(minSize, new Vector2D<int>(int.MaxValue, int.MaxValue));
    }

    public Color Background { get; set; } = new Color(0x202020);

    /// <summary>
    /// Gets the average frame time of the window.
    /// </summary>
    public float FrameTime => _frameTime.Average;

    public event Action<GraphicsDevice>? Load;
    public event Action<Canvas, float>? Render;
    public event Action? Unload;
    public event Action? Resize;
    public event Action<Key>? KeyDown;
    public event Action<Vector2, MouseButton>? MouseDown;

    public void Close() => _window.Close();

    public void Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _window.Run();
    }

    private void HandleLoad()
    {
        var gl = GL.GetApi(_window);
        _canvas = new Canvas(gl, new SilkViewport(_window));
        var input = _window.CreateInput();
        foreach (var kb in input.Keyboards)
            kb.KeyDown += (_, key, _) => KeyDown?.Invoke(key);
        foreach (var mouse in input.Mice)
            mouse.MouseDown += (m, btn) =>
                MouseDown?.Invoke(_canvas.WindowToFramebuffer(m.Position), btn);
        Load?.Invoke(new GraphicsDevice(gl));
    }

    private void HandleRender(Canvas r, float dt)
    {
        _frameTime.Add(dt);
        r.Clear(Background);
        r.Begin(dt);
        Render?.Invoke(r, dt);
        r.End();
    }

    private void HandleClosing()
    {
        Unload?.Invoke();
        _canvas?.Dispose();
        _canvas = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        HandleClosing();
        _window.Dispose();
        _disposed = true;
    }
}
