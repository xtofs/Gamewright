using System.Runtime.InteropServices;
using Gamewright;

namespace Gamewright.Tests;

// main.ts decodes frames with hard-coded numbers; these tests pin the values it relies on.
public class ProtocolTests
{
    [Fact]
    public void StyleIsSixteenBytesWithTheFieldOrderMainTsReads()
    {
        Style[] styles = [new Style(0x11223344u, 0x55667788u, 1.5f, 7)];
        var bytes = MemoryMarshal.AsBytes<Style>(styles);

        Assert.Equal(16, bytes.Length);
        Assert.Equal(0x11223344u, BitConverter.ToUInt32(bytes[..4]));
        Assert.Equal(0x55667788u, BitConverter.ToUInt32(bytes[4..8]));
        Assert.Equal(1.5f, BitConverter.ToSingle(bytes[8..12]));
        Assert.Equal(7, BitConverter.ToInt32(bytes[12..16]));
    }

    [Fact]
    public void DrawCommandIsTwentySixPackedBytesInTheOrderMainTsReads()
    {
        DrawCommand[] commands = [new(DrawCommandType.Text, 0x0102, 0x0304, 5, 6, 7, 8, 9)];
        var bytes = MemoryMarshal.AsBytes<DrawCommand>(commands);

        Assert.Equal(26, bytes.Length);
        Assert.Equal(2, BitConverter.ToUInt16(bytes[..2]));
        Assert.Equal(0x0102, BitConverter.ToUInt16(bytes[2..4]));
        Assert.Equal(0x0304, BitConverter.ToUInt16(bytes[4..6]));
        for (var i = 0; i < 5; i++)
            Assert.Equal(5 + i, BitConverter.ToInt32(bytes[(6 + 4 * i)..(10 + 4 * i)]));
    }

    [Fact]
    public void EnumValuesMatchTheRenderer()
    {
        Assert.Equal([1, 2, 3], Enum.GetValues<DrawCommandType>().Select(static v => (int)v));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5],
            Enum.GetValues<PathOpcode>().Select(static v => (int)v));
        Assert.Equal(1, (int)DrawCommandFlags.Fill);
        Assert.Equal(2, (int)DrawCommandFlags.Stroke);
        Assert.Equal(4, (int)DrawCommandFlags.Closed);
        Assert.Equal(8, (int)DrawCommandFlags.AlignCenter);
        Assert.Equal(16, (int)DrawCommandFlags.AlignEnd);
        Assert.Equal(0, (int)FontIndex.Body);
        Assert.Equal(1, (int)FontIndex.Heading);
    }

    [Fact]
    public void ColorPacksAsRgbaLittleEndian() =>
        Assert.Equal(0x44332211u, new Color(0x11, 0x22, 0x33, 0x44).Rgba);
}
