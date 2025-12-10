using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class LongestConstraint(string suit) : IBidConstraint
{
    public Suit Suit = suit.ToSuit(); // "hearts", "spades", etc

    public bool IsMet(DecisionContext ctx)
    {
        var shape = ctx.HandEvaluation.Shape;
        var length = shape[Suit];

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            if (suit == Suit) continue;
            
            if(shape[suit] > length)
                return false;
        }

        return true;

    }


}