using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidOwnSuit : BiddingRuleBase
{
    public override string Name { get; } = "Acol rebid own suit";
    public override int Priority { get; } = 42;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 2)
        {
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                return true;
        }
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.IsBalanced)
            return false;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        return ctx.HandEvaluation.Shape[firstBidSuit] >= 6;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var suit = ctx.AuctionEvaluation.OpeningBid!.Suit;

        if (suit == null)
            return null;

        var suitValue = (Suit)suit;
        var nextLevel = GetNextSuitBidLevel(suitValue, ctx.AuctionEvaluation.CurrentContract);

        // Jump rebid (e.g. 1H-1S-3H) shows a strong 6+ card suit (16+ HCP).
        // Only available when minimum rebid is still at the 2-level.
        if (nextLevel <= 2 && hcp >= 16)
            return Bid.SuitBid(nextLevel + 1, suitValue);

        return Bid.SuitBid(nextLevel, suitValue);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        // Must be the same suit as the opening bid
        return ctx.AuctionEvaluation.OpeningBid!.Suit == bid.Suit;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Suit == null)
            return null;

        var nextLevel = GetNextSuitBidLevel((Suit)bid.Suit, ctx.AuctionEvaluation.CurrentContract);

        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(ctx.AuctionEvaluation.OpeningBid!.Suit, 6, 10));

        // Minimum rebid at any level = minimum opener (12-15)
        // Jump rebid (bid.Level > nextLevel) = strong hand (16-19)
        if (bid.Level == nextLevel)
            constraints.Add(new HcpConstraint(12, 15));
        else
            constraints.Add(new HcpConstraint(16, 19));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
