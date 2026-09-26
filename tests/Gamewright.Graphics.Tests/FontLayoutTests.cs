namespace Gamewright.Graphics.Tests;

using System.Numerics;
using System.Text;
using Gamewright.Graphics;

public sealed class FontLayoutTests
{
    [Fact]
    public void Layout_AppliesBearingAdvanceScaleAndNewlines()
    {
        var glyph = new FontGlyph(
            new TextureRegion(Vector2.Zero, Vector2.One),
            new Vector2(10, 12),
            new Vector2(2, 3),
            14);
        var glyphs = new Dictionary<Rune, FontGlyph>
        {
            [new Rune('A')] = glyph,
            [new Rune('?')] = glyph,
        };

        var layout = FontLayout.Layout("A?\nA", new Vector2(5, 7), 2, 20, glyphs, new Rune('?'));

        Assert.Equal(3, layout.Count);
        Assert.Equal(new Rect(9, 13, 20, 24), layout[0].Destination);
        Assert.Equal(new Rect(37, 13, 20, 24), layout[1].Destination);
        Assert.Equal(new Rect(9, 53, 20, 24), layout[2].Destination);
    }

    [Fact]
    public void Layout_UsesFallbackForUnknownRuneAndTabsAdvanceWithoutQuad()
    {
        var glyph = new FontGlyph(TextureRegion.Full, new Vector2(8, 8), Vector2.Zero, 10);
        var glyphs = new Dictionary<Rune, FontGlyph> { [new Rune('?')] = glyph };

        var layout = FontLayout.Layout("?\tX", Vector2.Zero, 1, 12, glyphs, new Rune('?'));

        Assert.Equal(2, layout.Count);
        Assert.Equal(new Rect(0, 0, 8, 8), layout[0].Destination);
        Assert.Equal(new Rect(50, 0, 8, 8), layout[1].Destination);
    }
}
