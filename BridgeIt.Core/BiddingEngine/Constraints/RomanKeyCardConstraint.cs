using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class RomanKeyCardConstraint(int number) : IBidConstraint
{
    public bool IsMet(DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.BestFitSuit(ctx.HandEvaluation.Shape) == null) return false;
        var keyCards = ctx.HandEvaluation.RomanKeyCardCount[ctx.PartnershipKnowledge.BestFitSuit(ctx.HandEvaluation.Shape)!.Value];
        
        return keyCards == number;
    }
}