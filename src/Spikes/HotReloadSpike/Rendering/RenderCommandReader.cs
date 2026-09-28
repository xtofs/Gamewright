namespace HotReloadSpike.Rendering;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotReloadSpike.Commands;

/// <summary>
/// Walks the bytes produced by a <see cref="RenderCommandBuffer"/>: call <see cref="TryReadOp"/>
/// and then <see cref="Read{T}"/> with the struct type that belongs to the returned opcode.
/// </summary>
public ref struct RenderCommandReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _offset;

    public RenderCommandReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _offset = 0;
    }

    public bool TryReadOp(out RenderOp op)
    {
        if (_offset >= _data.Length)
        {
            op = RenderOp.None;
            return false;
        }

        op = (RenderOp)_data[_offset++];
        return true;
    }

    public T Read<T>()
        where T : unmanaged
    {
        var value = MemoryMarshal.Read<T>(_data[_offset..]);
        _offset += Unsafe.SizeOf<T>();
        return value;
    }
}
