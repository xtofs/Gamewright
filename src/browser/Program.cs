using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Gamewright;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "start")
            StopwatchSample.Start();

        while (true)
        {
            StopwatchSample.Render();
            await Task.Delay(1000);
        }
    }
}

partial class Api
{
    [JSImport("dom.submit", "main.js")]
    internal static partial void Submit(
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] commands,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] pathData,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[] geometry,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] strings,
        [JSMarshalAs<JSType.Array<JSType.Number>>] int[] styles);
}

partial class StopwatchSample
{
    private static readonly Stopwatch stopwatch = new();

    public static void Start() => stopwatch.Start();

    public static void Render()
    {
        var frame = new FrameBuilder()
            .AddPath(
                path => path.MoveTo(20, 20)
                    .LineTo(620, 20)
                    .LineTo(620, 140)
                    .LineTo(20, 140)
                    .Close(),
                fillColor: new Color(23, 32, 51),
                strokeStyle: new StrokeStyle(new Color(82, 97, 122), 2))
            .AddPath(
                "M 40 80 C 110 20 170 140 240 80 S 370 20 440 80 S 570 140 600 80",
                strokeStyle: new StrokeStyle(new Color(99, 210, 163), 5))
            .AddText(
                $"Elapsed: {stopwatch.Elapsed:mm\\:ss}",
                40,
                190,
                new Color(242, 245, 250),
                FontIndex.Heading,
                TextAlignment.Start)
            .Build();

        Api.Submit(
            frame.Commands.SelectMany(static command => new[]
            {
                (int)command.Type,
                command.StyleIndex,
                command.Flags,
                command.GeometryOffset,
                command.GeometryLength,
                command.PathDataOffset,
                command.PathDataLength,
                command.Data
            }).ToArray(),
            Array.ConvertAll(frame.PathData, static value => (int)value),
            Array.ConvertAll(frame.Geometry, static value => (double)value),
            frame.Strings,
            MemoryMarshal.AsBytes(frame.Styles.AsSpan())
                .ToArray()
                .Select(static value => (int)value)
                .ToArray());
    }

    [JSExport]
    internal static bool Toggle()
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
            return false;
        }

        stopwatch.Start();
        return true;
    }

    [JSExport]
    internal static void Reset()
    {
        if (stopwatch.IsRunning)
            stopwatch.Restart();
        else
            stopwatch.Reset();

        Render();
    }

    [JSExport]
    internal static bool IsRunning() => stopwatch.IsRunning;
}
