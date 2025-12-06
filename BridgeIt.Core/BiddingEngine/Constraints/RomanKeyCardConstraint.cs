using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class RomanKeyCardConstraint(int number) : IBidConstraint
{
    public bool IsMet(BiddingContext ctx)
    {
        var keyCards = ctx.HandEvaluation.RKCB[ctx.PartnershipKnowledge.BestFitSuit(ctx.HandEvaluation.Shape)!.Value];
        
        return keyCards == number;
    }
}