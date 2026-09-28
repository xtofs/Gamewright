namespace HotReloadSpike.Tests;

using System.Numerics;
using HotReloadSpike.Commands;
using HotReloadSpike.Threading;

public class FrameExchangeTests
{
    [Fact]
    public void AcquireLatest_ReturnsLastPublishedFrame()
    {
        var frames = new FrameExchange();

        frames.BeginWrite().Write(ClearColorCommand.Create(Vector4.One));
        frames.Publish();
        var second = frames.BeginWrite();
        second.Write(ClearColorCommand.Create(Vector4.One));
        second.Write(ClearColorCommand.Create(Vector4.One));
        frames.Publish();

        var front = frames.AcquireLatest();

        Assert.Same(second, front);
        Assert.Equal(2, front.Count);
    }

    [Fact]
    public void AcquireLatest_WithoutNewPublish_ReturnsSameFrame()
    {
        var frames = new FrameExchange();
        frames.BeginWrite().Write(ClearColorCommand.Create(Vector4.One));
        frames.Publish();

        var first = frames.AcquireLatest();
        var again = frames.AcquireLatest();

        Assert.Same(first, again);
        Assert.Equal(1, again.Count);
    }

    [Fact]
    public void BeginWrite_NeverReturnsTheFrontBuffer()
    {
        var frames = new FrameExchange();
        for (var i = 0; i < 5; i++)
        {
            frames.BeginWrite();
            frames.Publish();
            var front = frames.AcquireLatest();

            Assert.NotSame(front, frames.BeginWrite());
        }
    }
}
