using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class TemplateRule : BiddingRuleBase
{
    public override string Name { get; }
    public override int Priority { get; }


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        throw new NotImplementedException();
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        throw new NotImplementedException();
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        throw new NotImplementedException();
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        throw new NotImplementedException();
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        throw new NotImplementedException();
    }
}