namespace Gamewright.Graphics;

using System.Numerics;
using Silk.NET.Input;

/// <summary>
/// A game's state together with how it renders and reacts to input, run by
/// <see cref="Window.Run(IScene)"/>. Only <see cref="Render"/> is required; the other members
/// default to doing nothing.
/// </summary>
/// <remarks>
/// A scene that needs the window, e.g. to close it or to read <see cref="Window.FrameTime"/>,
/// takes it as a constructor argument.
/// </remarks>
public interface IScene
{
    /// <summary>Called once the GL context exists; create textures and fonts here.</summary>
    void Load(GraphicsDevice device)
    {
    }

    /// <summary>Called when the window closes; dispose what <see cref="Load"/> created.</summary>
    void Unload()
    {
    }

    /// <summary>Called every frame, between <see cref="Canvas.Begin"/> and <see cref="Canvas.End"/>.</summary>
    void Render(Canvas canvas, float deltaTime);

    void KeyDown(Key key)
    {
    }

    /// <param name="position">In framebuffer pixels, the same space <see cref="Canvas"/> draws in.</param>
    void MouseDown(Vector2 position, MouseButton button)
    {
    }
}
