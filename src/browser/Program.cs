using Chess;

internal class Program
{
    private static void Main()
    {
        try
        {
            AppHost.Start(new ChessApp());
        }
        catch (Exception exception)
        {
            Api.ReportError(exception.ToString());
            throw;
        }
    }
}
