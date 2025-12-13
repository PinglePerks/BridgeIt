namespace BridgeIt.Core.Domain.Primatives;

public class Deck
{
    private readonly List<Card> _cards = new();

    public IReadOnlyList<Card> Cards => _cards;

    private static readonly Random Rng = new();

    public Deck()
    {
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                _cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        for (var i = _cards.Count - 1; i > 0; i--)
        {
            var j = Rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }
}