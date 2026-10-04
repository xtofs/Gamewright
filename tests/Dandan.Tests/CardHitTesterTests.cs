namespace Dandan.Tests;

using System.Numerics;

public sealed class CardHitTesterTests
{
    [Fact]
    public void TryHitTest_UsesLayoutCardSize()
    {
        var layout = new TestLayout(new(600, 800), [new(0, 0)]);
        var tester = new CardHitTester(layout);

        Assert.True(tester.TryHitTest([0], new(200, 0), out var slot));
        Assert.Equal(0, slot);
    }

    [Fact]
    public void TryHitTest_MapsDrawOrderToLayoutSlots()
    {
        var layout = new TestLayout(new(100, 100), [new(0, 0), new(0, 0), new(2000, 0)]);
        var tester = new CardHitTester(layout);

        Assert.True(tester.TryHitTest([1, 0, 2], Vector2.Zero, out var slot));
        Assert.Equal(0, slot);
    }

    [Fact]
    public void CardInFocusOrder_DrawsFocusedSlotLast()
    {
        Assert.Equal([0, 6, 1, 5, 2, 4, 3], Scene.CardInFocusOrder(7, 3));
    }

    private sealed class TestLayout(Vector2 cardSize, Vector2[] centers) : ICardLayout
    {
        public Vector2 CardSize => cardSize;

        public CardPlacement GetPlacement(int slot, int count) => new(centers[slot], 0);
    }
}
