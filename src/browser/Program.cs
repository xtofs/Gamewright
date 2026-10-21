using Gamewright;

internal class Program
{
    private static void Main()
    {
        try
        {
            Api.ConfigureFrame(AppHost.FrameWidth, AppHost.FrameHeight);
            Api.ConfigureSprites(SpriteSheet.Columns, SpriteSheet.CellSize);
            AppHost.Render(Invalidation.All);
        }
        catch (Exception exception)
        {
            Api.ReportError(exception.ToString());
            throw;
        }
    }
}
