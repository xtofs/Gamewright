using Gamewright;

internal class Program
{
    private static void Main()
    {
        try
        {
            Api.ConfigureFrame(ChessGame.FrameWidth, ChessGame.FrameHeight);
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
