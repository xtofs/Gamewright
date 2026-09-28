namespace Gamewright.Graphics;

using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Gamewright.Graphics.Utilities;
using SilkWindow = Silk.NET.Windowing.Window;
using Silk.NET.Windowing;

public sealed class Window : IDisposable
{
    private readonly IWindow _window;
    private Canvas? _canvas;
    private readonly ExponentialMovingAverage _frameTime = new(100);
    private string? _lastRenderError;
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

    /// <summary>Runs the window with <paramref name="scene"/> handling its events.</summary>
    public void Run(IScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Load += scene.Load;
        Render += scene.Render;
        Unload += scene.Unload;
        KeyDown += scene.KeyDown;
        MouseDown += scene.MouseDown;
        try
        {
            Run();
        }
        finally
        {
            Load -= scene.Load;
            Render -= scene.Render;
            Unload -= scene.Unload;
            KeyDown -= scene.KeyDown;
            MouseDown -= scene.MouseDown;
        }
    }

    private void HandleLoad()
    {
        var gl = GL.GetApi(_window);
        _canvas = new Canvas(gl, new SilkViewport(_window));
        var input = _window.CreateInput();

        void Keyboard(IKeyboard _1, Key key, int _2) => Guarded(nameof(KeyDown), () => KeyDown?.Invoke(key));
        foreach (var kb in input.Keyboards)
        {
            kb.KeyDown += Keyboard;
        }

        void MouseHandler(IMouse _, MouseButton btn) => Guarded(nameof(MouseDown), () => MouseDown?.Invoke(_canvas.WindowToFramebuffer(_.Position), btn));
        foreach (var mouse in input.Mice)
        {
            mouse.MouseDown += MouseHandler;
        }

        Load?.Invoke(new GraphicsDevice(gl));
    }

    private void HandleRender(Canvas r, float dt)
    {
        _frameTime.Add(dt);
        r.Clear(Background);
        r.Begin(dt);
        try
        {
            Render?.Invoke(r, dt);
            if (_lastRenderError is not null)
            {
                Console.WriteLine("Render recovered.");
                _lastRenderError = null;
            }
        }
        catch (Exception ex)
        {
            // Keep the window alive, e.g. while a Hot Reload edit is broken. Logged once per
            // distinct error, since this repeats every frame.
            var error = ex.ToString();
            if (error != _lastRenderError)
            {
                Console.Error.WriteLine($"Render failed: {error}");
                _lastRenderError = error;
            }
        }
        finally
        {
            // draws whatever was batched before the exception and leaves the canvas ready for
            // the next frame
            r.End();
        }
    }

    private static void Guarded(string handler, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{handler} failed: {ex}");
        }
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
