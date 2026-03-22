using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class RomanKeyCardConstraint(int number) : IBidConstraint
{
    public bool IsMet(DecisionContext ctx)
    {
        var fitSuit = ctx.BestFitSuit();
        if (fitSuit == null) return false;
        var keyCards = ctx.HandEvaluation.RomanKeyCardCount[fitSuit.Value];

        return keyCards == number;
    }
}