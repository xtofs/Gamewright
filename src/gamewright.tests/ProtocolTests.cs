using System.Runtime.InteropServices;
using Gamewright;

namespace Gamewright.Tests;

// main.ts decodes frames with hard-coded numbers; these tests pin the values it relies on.
public class ProtocolTests
{
    [Fact]
    public void PaintIsSixteenBytesWithTheFieldOrderMainTsReads()
    {
        Paint[] paints = [Paint.Fill(new Color(0x11, 0x22, 0x33, 0x44)).WithStroke(new Color(0x55, 0x66, 0x77, 0x88), 1.5f)];
        var bytes = MemoryMarshal.AsBytes<Paint>(paints);

        Assert.Equal(16, bytes.Length);
        Assert.Equal(0x44332211u, BitConverter.ToUInt32(bytes[..4]));
        Assert.Equal(0x88776655u, BitConverter.ToUInt32(bytes[4..8]));
        Assert.Equal(1.5f, BitConverter.ToSingle(bytes[8..12]));
        Assert.Equal(3u, BitConverter.ToUInt32(bytes[12..16]));
    }

    [Fact]
    public void TextStyleIsEightBytesWithTheFieldOrderMainTsReads()
    {
        TextStyle[] styles = [new TextStyle(0x11223344u, (int)FontIndex.Heading)];
        var bytes = MemoryMarshal.AsBytes<TextStyle>(styles);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(0x11223344u, BitConverter.ToUInt32(bytes[..4]));
        Assert.Equal(1, BitConverter.ToInt32(bytes[4..8]));
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
        Assert.Equal(1, (int)DrawCommandFlags.Closed);
        Assert.Equal(2, (int)DrawCommandFlags.AlignCenter);
        Assert.Equal(4, (int)DrawCommandFlags.AlignEnd);
        Assert.Equal(
            [0u, 1, 2, 3, 4],
            new[] { PaintMode.None, PaintMode.Fill, PaintMode.Stroke, PaintMode.FillThenStroke, PaintMode.StrokeThenFill }
                .Select(static m => (uint)m));
        Assert.Equal(0, (int)FontIndex.Body);
        Assert.Equal(1, (int)FontIndex.Heading);
    }

    [Fact]
    public void ColorPacksAsRgbaLittleEndian() =>
        Assert.Equal(0x44332211u, new Color(0x11, 0x22, 0x33, 0x44).Rgba);
}
