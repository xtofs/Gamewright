namespace Gamewright.Rendering;

using Silk.NET.OpenGL;

internal readonly record struct InstanceAttribute(uint Location, int ComponentCount, int Offset);

internal sealed unsafe class InstanceBatch<T> : IDisposable
    where T : unmanaged
{
    private readonly GL gl;
    private T[] instances;
    private uint vertexArray;
    private uint quadBuffer;
    private uint indexBuffer;
    private uint instanceBuffer;
    private int gpuCapacity;
    private bool disposed;

    public InstanceBatch(GL gl, IReadOnlyList<InstanceAttribute> attributes, int initialCapacity = 128)
    {
        this.gl = gl;
        instances = new T[initialCapacity];
        CreateBuffers(attributes);
    }

    public int Count { get; private set; }

    public void Add(T instance)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Count == instances.Length)
        {
            Array.Resize(ref instances, checked(instances.Length * 2));
        }

        instances[Count++] = instance;
    }

    public void Draw(ShaderProgram program)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Count == 0)
        {
            return;
        }

        program.Use();
        gl.BindVertexArray(vertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceBuffer);
        if (instances.Length > gpuCapacity)
        {
            gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(instances.Length * sizeof(T)),
                null,
                BufferUsageARB.DynamicDraw);
            gpuCapacity = instances.Length;
        }

        fixed (T* data = instances)
        {
            gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(Count * sizeof(T)), data);
        }

        gl.DrawElementsInstanced(
            PrimitiveType.Triangles,
            6,
            DrawElementsType.UnsignedInt,
            null,
            (uint)Count);
        Count = 0;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gl.DeleteBuffer(instanceBuffer);
        gl.DeleteBuffer(indexBuffer);
        gl.DeleteBuffer(quadBuffer);
        gl.DeleteVertexArray(vertexArray);
        disposed = true;
    }

    private void CreateBuffers(IReadOnlyList<InstanceAttribute> attributes)
    {
        ReadOnlySpan<float> vertices = [-1, -1, 1, -1, 1, 1, -1, 1];
        ReadOnlySpan<uint> indices = [0, 1, 2, 2, 3, 0];

        vertexArray = gl.GenVertexArray();
        quadBuffer = gl.GenBuffer();
        indexBuffer = gl.GenBuffer();
        instanceBuffer = gl.GenBuffer();

        gl.BindVertexArray(vertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, quadBuffer);
        fixed (float* data = vertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
        }

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);
        fixed (uint* data = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), data, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceBuffer);
        gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            (nuint)(instances.Length * sizeof(T)),
            null,
            BufferUsageARB.DynamicDraw);
        gpuCapacity = instances.Length;
        foreach (var attribute in attributes)
        {
            gl.EnableVertexAttribArray(attribute.Location);
            gl.VertexAttribPointer(
                attribute.Location,
                attribute.ComponentCount,
                VertexAttribPointerType.Float,
                false,
                (uint)sizeof(T),
                (void*)attribute.Offset);
            gl.VertexAttribDivisor(attribute.Location, 1);
        }

        gl.BindVertexArray(0);
    }
}
