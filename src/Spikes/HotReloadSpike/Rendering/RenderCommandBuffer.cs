namespace HotReloadSpike.Rendering;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotReloadSpike.Commands;

/// <summary>
/// A flat byte stream of render commands: for each command, one <see cref="RenderOp"/> byte
/// followed by the raw bytes of the command struct. Commands are packed without padding, so
/// they are written and read unaligned.
/// </summary>
public sealed class RenderCommandBuffer
{
    private byte[] _buffer;
    private int _offset;

    public RenderCommandBuffer(int capacity = 4096)
        => _buffer = new byte[capacity];

    /// <summary>Number of commands written since the last <see cref="Clear"/>.</summary>
    public int Count { get; private set; }

    public ReadOnlySpan<byte> Data => _buffer.AsSpan(0, _offset);

    public void Clear()
    {
        _offset = 0;
        Count = 0;
    }

    public void Write<T>(T cmd)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        EnsureCapacity(1 + size);

        // Write opcode
        _buffer[_offset++] = (byte)OpCodeOf<T>.Value;

        // Write struct bytes
        MemoryMarshal.Write(_buffer.AsSpan(_offset, size), in cmd);
        _offset += size;
        Count++;
    }

    private void EnsureCapacity(int additional)
    {
        var required = _offset + additional;
        if (required > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(required, _buffer.Length * 2));
        }
    }

    /// <summary>
    /// Reads the <c>public const RenderOp OpCode</c> of a command struct once per type,
    /// instead of reflecting on every <see cref="Write{T}"/>.
    /// </summary>
    private static class OpCodeOf<T>
    {
        public static readonly RenderOp Value = Resolve();

        private static RenderOp Resolve()
        {
            var field = typeof(T).GetField("OpCode", BindingFlags.Public | BindingFlags.Static);
            if (field is not { IsLiteral: true } || field.FieldType != typeof(RenderOp))
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} must declare 'public const RenderOp OpCode' to be written to a {nameof(RenderCommandBuffer)}.");
            }

            var op = (RenderOp)Convert.ToByte(field.GetRawConstantValue());
            if (op == RenderOp.None)
            {
                throw new InvalidOperationException($"{typeof(T).Name}.OpCode must not be {nameof(RenderOp.None)}.");
            }

            return op;
        }
    }
}
