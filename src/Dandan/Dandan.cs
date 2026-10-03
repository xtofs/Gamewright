namespace Dandan;

public static class Dandan
{
    // private static readonly Dictionary<Card, int> CardQuantities = new()
    // {
    //     [Card.Dandan] = 10,
    // };

      private static readonly (Card Card, int Count)[] CardQuantities = [
        // Creatures (10)
        (Card.Dandan, 10),

        // Instants (34)
        (Card.AccumulatedKnowledge, 4),
        (Card.Brainstorm, 2),
        (Card.CrystalSpray, 2),
        (Card.DanceOfTheSkywise, 2),
        (Card.MemoryLapse, 8),
        (Card.Metamorphose, 2),
        (Card.MindBend, 2),
        (Card.MysticalTutor, 2),
        (Card.Predict, 2),
        (Card.RayOfCommand, 2),
        (Card.SupplantForm, 2),
        (Card.Unsubstantiate, 2),
        (Card.VisionCharm, 2),

        // Sorceries (4)
        (Card.DiminishingReturns, 2),
        (Card.MysticRetrieval, 2),

        // Lands (32)
        (Card.HalimarDepths, 2),
        (Card.Island, 18),
        (Card.IzzetBoilerworks, 2),
        (Card.LonelySandbar, 2),
        (Card.MysticSanctuary, 2),
        (Card.RemoteIsle, 2),
        (Card.SvyeluniteTemple, 2),
        (Card.TempleOfEpiphany, 2),
    ];



    public static Deck CreateDeck()
    {
        return new Deck(CardQuantities);
    }
}

public class Deck(IEnumerable<(Card Card, int Count)> quantities)
{
    private Card[] _cards = [.. from kvp in quantities from c in kvp.Repeat() select c];

    public IList<Card> Draw(int count)
    {
        var result = _cards[0..count];
        _cards = _cards[count..];
        return result;
    }

    internal void Shuffle(Random rng)
    {
        rng.Shuffle(_cards);
    }
}

