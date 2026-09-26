namespace Gamewright.Graphics.Rendering;

using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

internal sealed unsafe class FrameUniformBuffer : IDisposable
{
    private readonly GL _gl;
    private bool _disposed;

    public FrameUniformBuffer(GL gl, uint binding)
    {
        this._gl = gl;
        Handle = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.UniformBuffer, Handle);
        gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(FrameData), null, BufferUsageARB.DynamicDraw);
        gl.BindBufferBase(BufferTargetARB.UniformBuffer, binding, Handle);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    public uint Handle { get; private set; }

    public void Update(Matrix4x4 projection, Vector2 viewportSize, float time)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = new FrameData
        {
            Projection = projection,
            ViewportSize = viewportSize,
            Time = time,
        };

        _gl.BindBuffer(BufferTargetARB.UniformBuffer, Handle);
        _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)sizeof(FrameData), &data);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gl.DeleteBuffer(Handle);
        Handle = 0;
        _disposed = true;
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    private struct FrameData
    {
        [FieldOffset(0)]
        public Matrix4x4 Projection;

        [FieldOffset(64)]
        public Vector2 ViewportSize;

        [FieldOffset(72)]
        public float Time;
    }
}
