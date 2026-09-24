namespace Gamewright;

using System.Numerics;
using Silk.NET.Windowing;

public sealed class SilkViewport(IWindow window) : IViewport
{
    public Vector2 FramebufferSize => window.FramebufferSize.ToVector2();
    public Vector2 WindowSize => window.Size.ToVector2();
}
