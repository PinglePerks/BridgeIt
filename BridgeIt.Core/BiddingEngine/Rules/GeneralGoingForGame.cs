using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class GeneralGoingForGame : BiddingRuleBase
{
    public override string Name { get; } = "General Going For Game";
    public override int Priority { get; } = 25;
    public override bool IsApplicable(DecisionContext ctx)
    {
        return ctx.AuctionEvaluation.CurrentContract != null;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        throw new NotImplementedException();
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        throw new NotImplementedException();
    }
}