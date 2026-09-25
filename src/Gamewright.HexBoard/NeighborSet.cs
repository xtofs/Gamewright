namespace Gamewright.HexBoard;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



/// <summary>
/// Represents a set of neighboring hex cells
/// as as compact fixed size value type
/// </summary>
/// 
[InlineArray(7)] // 7 * 2 byte = 112 bits < 128 bits = 16 bytes (Int128)
public struct NeighborSet
{
    private short _element0;

    public ReadOnlySpan<short> AsReadOnlySpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref this[1], this[0]);
    }

    public override string ToString() => $"{{{AsReadOnlySpan().JoinToString()}}}";

    internal void Add(short neighbor)
    {
        Debug.Assert(this[0] < 7);
        for (var i = this[0]; i > 0; i--)
        {
            if (this[i] == neighbor) { return; }
        }
        this[1 + this[0]] = (short)neighbor;
        this[0] += 1;
    }
}


static class StringExtensions
{
    extension<T>(ReadOnlySpan<T> span)
    {
        public string JoinToString(string separator = ",")
        {
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < span.Length; i++)
            {
                if (i > 0)
                {
                    result.Append(separator);
                }

                result.Append(span[i]?.ToString());
            }
            return result.ToString();
        }
    }
}


