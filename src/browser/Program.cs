using Gamewright;

internal class Program
{
    private static void Main()
    {
        try
        {
            Api.ConfigureSprites(SpriteSheet.Columns, SpriteSheet.CellSize);
            ChessGame.RenderStatic();
            ChessGame.Render();
        }
        catch (Exception exception)
        {
            Api.ReportError(exception.ToString());
            throw;
        }
    }
}
