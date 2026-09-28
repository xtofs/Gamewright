namespace HotReloadSpike.Rendering;

using System.Numerics;
using System.Runtime.InteropServices;
using HotReloadSpike.Resources;
using Silk.NET.OpenGL;

/// <summary>
/// GL state the command structs execute against: shader programs and one instance batch per
/// primitive. Consecutive commands of the same kind (and, for sprites, the same texture) are
/// batched into a single draw call; any change flushes the previous batch, so draw order is kept.
/// </summary>
public sealed class RenderContext : IDisposable
{
    private readonly ShaderProgram _roundedBoxProgram;
    private readonly ShaderProgram _capsuleProgram;
    private readonly ShaderProgram _triangleProgram;
    private readonly ShaderProgram _spriteProgram;
    private readonly InstanceBatch<RoundedBoxInstance> _roundedBoxes;
    private readonly InstanceBatch<CapsuleInstance> _capsules;
    private readonly InstanceBatch<TriangleInstance> _triangles;
    private readonly InstanceBatch<SpriteInstance> _sprites;
    private readonly TextureStore _textures;
    private BatchKind _activeBatch;
    private uint _activeTexture;

    public RenderContext(GL gl, TextureStore textures)
    {
        Gl = gl;
        _textures = textures;
        _roundedBoxProgram = new ShaderProgram(gl, Shaders.RoundedBoxVertex, Shaders.RoundedBoxFragment);
        _capsuleProgram = new ShaderProgram(gl, Shaders.CapsuleVertex, Shaders.CapsuleFragment);
        _triangleProgram = new ShaderProgram(gl, Shaders.TriangleVertex, Shaders.TriangleFragment);
        _spriteProgram = new ShaderProgram(gl, Shaders.SpriteVertex, Shaders.SpriteFragment);
        _roundedBoxes = new InstanceBatch<RoundedBoxInstance>(gl, RoundedBoxAttributes);
        _capsules = new InstanceBatch<CapsuleInstance>(gl, CapsuleAttributes);
        _triangles = new InstanceBatch<TriangleInstance>(gl, TriangleAttributes);
        _sprites = new InstanceBatch<SpriteInstance>(gl, SpriteAttributes);
    }

    public GL Gl { get; }

    public void BeginFrame(Vector2 framebufferSize)
    {
        Gl.Viewport(0, 0, (uint)framebufferSize.X, (uint)framebufferSize.Y);
        Gl.Enable(EnableCap.Blend);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // default background until the first frame with a ClearColor command arrives
        Gl.ClearColor(0, 0, 0, 1);
        Gl.Clear(ClearBufferMask.ColorBufferBit);

        var projection = Matrix4x4.CreateOrthographicOffCenter(0, framebufferSize.X, framebufferSize.Y, 0, -1, 1);
        _roundedBoxProgram.SetProjection(projection);
        _capsuleProgram.SetProjection(projection);
        _triangleProgram.SetProjection(projection);
        _spriteProgram.SetProjection(projection);
        _activeBatch = BatchKind.None;
    }

    public void EndFrame() => Flush();

    /// <summary>Draws whatever is batched so far.</summary>
    public void Flush()
    {
        switch (_activeBatch)
        {
            case BatchKind.RoundedBox:
                _roundedBoxes.Draw(_roundedBoxProgram);
                break;
            case BatchKind.Capsule:
                _capsules.Draw(_capsuleProgram);
                break;
            case BatchKind.Triangle:
                _triangles.Draw(_triangleProgram);
                break;
            case BatchKind.Sprite:
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, _activeTexture);
                _sprites.Draw(_spriteProgram);
                break;
        }

        _activeBatch = BatchKind.None;
    }

    internal void AddRoundedBox(in RoundedBoxInstance instance)
    {
        SwitchBatch(BatchKind.RoundedBox);
        _roundedBoxes.Add(instance);
    }

    internal void AddCapsule(in CapsuleInstance instance)
    {
        SwitchBatch(BatchKind.Capsule);
        _capsules.Add(instance);
    }

    internal void AddTriangle(in TriangleInstance instance)
    {
        SwitchBatch(BatchKind.Triangle);
        _triangles.Add(instance);
    }

    internal void AddSprite(TextureId texture, in SpriteInstance instance)
    {
        // can't happen for frames from ResourceLoader (uploads are queued before the frame is
        // published), but a stale handle shouldn't crash the render thread
        if (!_textures.TryGet(texture, out var handle))
        {
            return;
        }

        SwitchBatch(BatchKind.Sprite, handle);
        _sprites.Add(instance);
    }

    public void Dispose()
    {
        _sprites.Dispose();
        _triangles.Dispose();
        _capsules.Dispose();
        _roundedBoxes.Dispose();
        _spriteProgram.Dispose();
        _triangleProgram.Dispose();
        _capsuleProgram.Dispose();
        _roundedBoxProgram.Dispose();
    }

    private void SwitchBatch(BatchKind kind, uint texture = 0)
    {
        if (_activeBatch != kind || _activeTexture != texture)
        {
            Flush();
            _activeBatch = kind;
            _activeTexture = texture;
        }
    }

    private static IReadOnlyList<InstanceAttribute> RoundedBoxAttributes { get; } =
    [
        Attribute<RoundedBoxInstance>(1, 2, nameof(RoundedBoxInstance.Center)),
        Attribute<RoundedBoxInstance>(2, 2, nameof(RoundedBoxInstance.HalfExtent)),
        Attribute<RoundedBoxInstance>(3, 1, nameof(RoundedBoxInstance.CornerRadius)),
        Attribute<RoundedBoxInstance>(4, 4, nameof(RoundedBoxInstance.FillColor)),
        Attribute<RoundedBoxInstance>(5, 4, nameof(RoundedBoxInstance.StrokeColor)),
        Attribute<RoundedBoxInstance>(6, 1, nameof(RoundedBoxInstance.StrokeWidth)),
    ];

    private static IReadOnlyList<InstanceAttribute> CapsuleAttributes { get; } =
    [
        Attribute<CapsuleInstance>(1, 2, nameof(CapsuleInstance.Start)),
        Attribute<CapsuleInstance>(2, 2, nameof(CapsuleInstance.End)),
        Attribute<CapsuleInstance>(3, 1, nameof(CapsuleInstance.Thickness)),
        Attribute<CapsuleInstance>(4, 4, nameof(CapsuleInstance.Color)),
    ];

    private static IReadOnlyList<InstanceAttribute> TriangleAttributes { get; } =
    [
        Attribute<TriangleInstance>(1, 2, nameof(TriangleInstance.V0)),
        Attribute<TriangleInstance>(2, 2, nameof(TriangleInstance.V1)),
        Attribute<TriangleInstance>(3, 2, nameof(TriangleInstance.V2)),
        Attribute<TriangleInstance>(4, 4, nameof(TriangleInstance.Color)),
    ];

    private static IReadOnlyList<InstanceAttribute> SpriteAttributes { get; } =
    [
        Attribute<SpriteInstance>(1, 4, nameof(SpriteInstance.Destination)),
        Attribute<SpriteInstance>(2, 4, nameof(SpriteInstance.Uv)),
        Attribute<SpriteInstance>(3, 4, nameof(SpriteInstance.Tint)),
    ];

    private static InstanceAttribute Attribute<T>(uint location, int components, string field)
        where T : unmanaged
        => new InstanceAttribute(location, components, Marshal.OffsetOf<T>(field).ToInt32());

    private enum BatchKind
    {
        None,
        RoundedBox,
        Capsule,
        Triangle,
        Sprite,
    }
}
