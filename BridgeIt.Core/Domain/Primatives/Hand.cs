namespace BridgeIt.Core.Domain.Primatives;

public class Hand
{
    private readonly List<Card> _cards = new();

    public IReadOnlyList<Card> Cards => _cards;
    
    public Hand(IEnumerable<Card> cards)
    {
        _cards = cards
            .OrderByDescending(c => c.Suit)
            .ThenByDescending(c => c.Rank)
            .ToList();
        
    }
    
    public override string ToString()
    {
        string FormatSuit(Suit suit) =>
            new string(
                _cards.Where(c => c.Suit == suit)
                    .OrderByDescending(c => c.Rank)
                    .Select(c => c.Rank.ShortName()[0])
                    .ToArray()
            );

        return $"{FormatSuit(Suit.Spades)} " +
               $"{FormatSuit(Suit.Hearts)} " +
               $"{FormatSuit(Suit.Diamonds)} " +
               $"{FormatSuit(Suit.Clubs)}";
    }
    
    public int CountSuit(Suit suit)
        => _cards.Count(c => c.Suit == suit);
    
    
}