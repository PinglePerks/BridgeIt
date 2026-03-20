using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol2NTOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT Opening";
    public override int Priority { get; } = 20; // Higher priority than a standard suit opening

    private const int MinHcp = 20;
    private const int MaxHcp = 22;

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.SeatRoleType != SeatRoleType.NoBids)
            return false;

        return ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp 
               && ctx.HandEvaluation.IsBalanced;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(2);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.SeatRoleType != SeatRoleType.NoBids)
            return false;
        
        return bid.Type == BidType.NoTrumps && bid.Level == 2;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        constraints.Add(new BalancedConstraint()); // Assuming you have this!
        
        return new BidInformation(bid, constraints, null);
    }
}