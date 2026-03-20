using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class AcolStrongOpening : BiddingRuleBase
{
    public override string Name { get; } = "Acol Strong Opening";
    public override int Priority { get; } = 19; // Higher priority than a standard suit opening

    private const int MinHcp = 20;
    private const int MaxHcp = 35;

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.Data.AuctionHistory.Bids.Any(b => b.Bid.Type != BidType.Pass))
            return false;

        return ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp 
               && ctx.HandEvaluation.IsBalanced;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.SuitBid(2, Suit.Clubs);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract != null) return false;
        
        return bid is { Type: BidType.Suit, Level: 2, Suit: Suit.Clubs };
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        
        return new BidInformation(bid, constraints, null);
    }
}