namespace HotReloadSpike.Rendering;

using Silk.NET.OpenGL;

internal readonly record struct InstanceAttribute(uint Location, int ComponentCount, int Offset);

/// <summary>
/// Draws a unit quad (-1..1) once per instance of <typeparamref name="T"/>. Reduced copy of
/// Gamewright.Graphics' InstanceBatch.
/// </summary>
internal sealed unsafe class InstanceBatch<T> : IDisposable
    where T : unmanaged
{
    private readonly GL _gl;
    private T[] _instances;
    private uint _vertexArray;
    private uint _quadBuffer;
    private uint _indexBuffer;
    private uint _instanceBuffer;
    private int _gpuCapacity;

    public InstanceBatch(GL gl, IReadOnlyList<InstanceAttribute> attributes, int initialCapacity = 128)
    {
        _gl = gl;
        _instances = new T[initialCapacity];
        CreateBuffers(attributes);
    }

    public int Count { get; private set; }

    public void Add(in T instance)
    {
        if (Count == _instances.Length)
        {
            Array.Resize(ref _instances, _instances.Length * 2);
        }

        _instances[Count++] = instance;
    }

    public void Draw(ShaderProgram program)
    {
        if (Count == 0)
        {
            return;
        }

        program.Use();
        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
        if (_instances.Length > _gpuCapacity)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_instances.Length * sizeof(T)), null, BufferUsageARB.DynamicDraw);
            _gpuCapacity = _instances.Length;
        }

        fixed (T* data = _instances)
        {
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(Count * sizeof(T)), data);
        }

        _gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)Count);
        Count = 0;
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_instanceBuffer);
        _gl.DeleteBuffer(_indexBuffer);
        _gl.DeleteBuffer(_quadBuffer);
        _gl.DeleteVertexArray(_vertexArray);
    }

    private void CreateBuffers(IReadOnlyList<InstanceAttribute> attributes)
    {
        ReadOnlySpan<float> vertices = [-1, -1, 1, -1, 1, 1, -1, 1];
        ReadOnlySpan<uint> indices = [0, 1, 2, 2, 3, 0];

        _vertexArray = _gl.GenVertexArray();
        _quadBuffer = _gl.GenBuffer();
        _indexBuffer = _gl.GenBuffer();
        _instanceBuffer = _gl.GenBuffer();

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadBuffer);
        fixed (float* data = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        fixed (uint* data = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), data, BufferUsageARB.StaticDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_instances.Length * sizeof(T)), null, BufferUsageARB.DynamicDraw);
        _gpuCapacity = _instances.Length;
        foreach (var attribute in attributes)
        {
            _gl.EnableVertexAttribArray(attribute.Location);
            _gl.VertexAttribPointer(
                attribute.Location,
                attribute.ComponentCount,
                VertexAttribPointerType.Float,
                false,
                (uint)sizeof(T),
                (void*)attribute.Offset);
            _gl.VertexAttribDivisor(attribute.Location, 1);
        }

        _gl.BindVertexArray(0);
    }
}
