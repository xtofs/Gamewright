namespace Dandan;

/// <summary>
/// Represents the fan of cards and manages the order of cards for rendering based on focus.
/// </summary>
/// <param name="handCount"></param>
internal class CardFan
{
    public CardFan(int handCount)
    {
        _handCount = handCount;
        _focus = _handCount / 2;
        Index = [.. from i in Enumerable.Range(0, _handCount) orderby Math.Abs(i - _focus) descending select i];
    }
    private readonly int _handCount;
    private int _focus;

    public int[] Index { get; private set; }

    public int Focus
    {
        get => _focus;
        set
        {
            _focus = value % _handCount;
            Index = [.. from i in Enumerable.Range(0, _handCount) orderby Math.Abs(i - _focus) descending select i];
        }
    }
}
