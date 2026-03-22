using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol1SuitOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1-Level Suit Opening";
    public override int Priority { get; } = 10; // Lower than NT openings, higher than Pass

    // Standard Acol usually caps a 1-level opening at 19 HCP (20+ is usually 2-level)
    private const int MinHcp = 12;
    private const int MaxHcp = 19; 

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp;

    public override Bid? Apply(DecisionContext ctx)
    {
        var longestSuit = ctx.HandEvaluation.LongestAndStrongest;

        // Acol 4-card major logic
        if (longestSuit is Suit.Hearts or Suit.Spades) return Bid.SuitBid(1, longestSuit);
        if (ctx.HandEvaluation.Shape[Suit.Spades] >= 5) return Bid.SuitBid(1, Suit.Spades);
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 5) return Bid.SuitBid(1, Suit.Hearts);

        return Bid.SuitBid(1, longestSuit);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit && bid.Level == 1;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        constraints.Add(new SuitLengthConstraint(bid.Suit.ToString()!, ">=4"));
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}