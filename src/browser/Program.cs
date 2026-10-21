using Gamewright;

internal class Program
{
    private static void Main()
    {
        try
        {
            Api.ConfigureFrame(AppHost.FrameWidth, AppHost.FrameHeight);
            if (AppHost.SpriteSheet is { } sheet)
                Api.ConfigureSprites(sheet.Columns, sheet.CellSize);
            AppHost.Render(Invalidation.All);
        }
        catch (Exception exception)
        {
            Api.ReportError(exception.ToString());
            throw;
        }
    }
}
