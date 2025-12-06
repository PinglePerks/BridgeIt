using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hands;

public static class HighCardPoints
{
    public static int Count(Hand hand)
    {
        return hand.Cards.Sum(c => c.Rank switch
            {
                Rank.Ace => 4,
                Rank.King => 3,
                Rank.Queen => 2,
                Rank.Jack => 1,
                _ => 0,
            }
        );
    }
}