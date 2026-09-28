namespace HotReloadSpike;

using HotReloadSpike.Game;
using HotReloadSpike.Host;
using HotReloadSpike.Resources;
using HotReloadSpike.Threading;

public static class Program
{
    public static void Main()
    {
        var frames = new FrameExchange();
        var channel = new HostChannel();
        var uploads = new TextureUploadQueue();
        var loop = new GameLoop(new DemoScene(), frames, channel, new ResourceLoader(uploads));

        using var host = new RenderHost("Hot Reload Spike", frames, channel, uploads);
        host.Closing += loop.Stop;

        loop.Start();
        host.Run();
    }
}
