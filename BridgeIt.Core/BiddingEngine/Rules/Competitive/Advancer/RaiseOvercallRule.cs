using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;

/// <summary>
/// Raise partner's overcall suit.
///   Simple raise: 3+ support, 8-11 HCP → raise by 1 level.
///   Jump raise: 4+ support, 0-7 HCP → raise by 2 levels (preemptive).
/// </summary>
public class RaiseOvercallRule : BiddingRuleBase
{
    public override string Name => "Raise Partner's Overcall";
    public override int Priority { get; }

    public RaiseOvercallRule(int priority = 11)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.MyLastNonPassBid == null           // I haven't bid yet (advancer)
           && auction.PartnerLastNonPassBid is { Type: BidType.Suit }; // Partner made a suit overcall

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var support = ctx.HandEvaluation.Shape[partnerSuit];
        var hcp = ctx.HandEvaluation.Hcp;

        // Jump raise: 4+ support, 0-7 HCP
        if (support >= 4 && hcp <= 7) return true;

        // Simple raise: 3+ support, 8-11 HCP
        if (support >= 3 && hcp >= 8 && hcp <= 11) return true;

        return false;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var support = ctx.HandEvaluation.Shape[partnerSuit];
        var hcp = ctx.HandEvaluation.Hcp;
        var currentContract = ctx.AuctionEvaluation.CurrentContract;

        var cheapestLevel = GetNextSuitBidLevel(partnerSuit, currentContract);

        // Jump raise (preemptive): 4+ support, weak
        if (support >= 4 && hcp <= 7)
        {
            var jumpLevel = cheapestLevel + 1;
            return jumpLevel <= 4 ? Bid.SuitBid(jumpLevel, partnerSuit) : null;
        }

        // Simple raise
        return cheapestLevel <= 3 ? Bid.SuitBid(cheapestLevel, partnerSuit) : null;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return false;
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid?.Suit;
        return bid.Suit == partnerSuit;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return null;
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid?.Suit;
        if (bid.Suit != partnerSuit || !partnerSuit.HasValue) return null;

        var cheapestLevel = GetNextSuitBidLevel(partnerSuit.Value, ctx.AuctionEvaluation.CurrentContract);
        var isJump = bid.Level > cheapestLevel;

        var constraints = new CompositeConstraint();
        if (isJump)
        {
            // Jump raise: 4+ support, 0-7 HCP
            constraints.Add(new SuitLengthConstraint(partnerSuit.Value, 4, 13));
            constraints.Add(new HcpConstraint(0, 7));
        }
        else
        {
            // Simple raise: 3+ support, 8-11 HCP
            constraints.Add(new SuitLengthConstraint(partnerSuit.Value, 3, 13));
            constraints.Add(new HcpConstraint(8, 11));
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => null; // No meaningful negative inference from advancer passing
}
