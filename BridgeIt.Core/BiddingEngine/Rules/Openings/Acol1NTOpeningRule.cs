using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

public class Acol1NTOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT Opening";
    public override int Priority { get; } = 20; // Higher priority than a standard suit opening

    private const int MinHcp = 12;
    private const int MaxHcp = 14;

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.Data.AuctionHistory.Bids.Any(b => b.Bid.Type != BidType.Pass))
            return false;

        return ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp 
               && ctx.HandEvaluation.IsBalanced;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(1);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract != null) return false;
        
        return bid is { Type: BidType.NoTrumps, Level: 2 };
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        constraints.Add(new BalancedConstraint()); // Assuming you have this!
        
        return new BidInformation(bid, constraints, null);
    }
}