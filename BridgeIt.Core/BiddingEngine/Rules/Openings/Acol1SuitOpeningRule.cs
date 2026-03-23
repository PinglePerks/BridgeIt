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
    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(MinHcp, 40) } };

    // Standard Acol usually caps a 1-level opening at 19 HCP (20+ is usually 2-level)
    private const int MinHcp = 12;
    private const int MaxHcp = 19; 

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp;

    public override Bid? Apply(DecisionContext ctx)
    {
        var fiveCardSuits = ctx.HandEvaluation.SuitsWithMinLength(5);

        // Two 5+ card suits: bid the higher-ranking first
        if (fiveCardSuits.Count >= 2)
            return Bid.SuitBid(1, fiveCardSuits.First());

        // One 5+ card suit: bid it (even if it's a minor — longest suit first)
        if (fiveCardSuits.Count == 1)
            return Bid.SuitBid(1, fiveCardSuits.First());

        // All 4-card suits: bid the longest (highest-ranking breaks ties)
        return Bid.SuitBid(1, ctx.HandEvaluation.LongestAndStrongest);
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