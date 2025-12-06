using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hands;

public static class KeyCardCalculator
{
    public static Dictionary<Suit, int> CalculateAll(Hand hand)
    {
        var result = new Dictionary<Suit, int>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            result.Add(suit, Calculate(hand, suit));
        }
        return result;
    }
    public static int Calculate(Hand hand, Suit suit)
    {
        var count = 0;
        foreach (var card in hand.Cards)
        {
            if (card.Rank == Rank.Ace) count++;
            if (card.Suit == suit && card.Rank == Rank.King) count++;
        }
        return count;
        
    }
}