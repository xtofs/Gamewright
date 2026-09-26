namespace Gamewright.Graphics;

using System.Numerics;

/// <summary>Provides the framebuffer and logical window dimensions the renderer needs each frame.</summary>
public interface IViewport
{
    /// <summary>Size of the framebuffer in physical pixels.</summary>
    Vector2 FramebufferSize { get; }

    /// <summary>Size of the window in logical (OS) pixels. Used to convert mouse coordinates to framebuffer space.</summary>
    Vector2 WindowSize { get; }
}
