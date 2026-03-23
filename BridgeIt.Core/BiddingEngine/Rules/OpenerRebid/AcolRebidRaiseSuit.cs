using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidRaiseSuit : BiddingRuleBase
{
    public override string Name { get; } = "Acol rebid partner's suit";
    public override int Priority { get; } = 35;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 2)
        {
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
            {
                if (auction.PartnerLastBid != null && auction.PartnerLastBid.Type == BidType.Suit)
                    return true;
            }
        }
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.IsBalanced)
            return false;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var shape = ctx.HandEvaluation.Shape;

        // If opener has 6+ in own suit, RebidOwnSuit handles it
        if (shape[firstBidSuit] >= 6)
            return false;

        var partnerSuit = ctx.AuctionEvaluation.PartnerLastBid!.Suit;
        if (partnerSuit == null || partnerSuit == firstBidSuit)
            return false;

        // Need 4-card support for partner's suit
        return shape[partnerSuit.Value] >= 4;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var raiseSuit = ctx.AuctionEvaluation.PartnerLastBid!.Suit!.Value;
        var nextLevel = GetNextSuitBidLevel(raiseSuit, ctx.AuctionEvaluation.CurrentContract);
        bool isMajor = raiseSuit == Suit.Hearts || raiseSuit == Suit.Spades;

        // 19+ HCP: game raise in a major
        if (hcp >= 19 && isMajor && nextLevel <= 2)
            return Bid.SuitBid(4, raiseSuit);

        // 16-18 HCP: invitational jump raise
        if (hcp >= 16 && nextLevel <= 2)
            return Bid.SuitBid(nextLevel + 1, raiseSuit);

        // 12-15 HCP: minimum raise
        return Bid.SuitBid(nextLevel, raiseSuit);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        // Must not be the same suit as opener's opening bid
        if (ctx.AuctionEvaluation.OpeningBid!.Suit == bid.Suit)
            return false;

        // Must match partner's suit (i.e. this is a raise)
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        if (partnerBid == null || partnerBid.Type != BidType.Suit || partnerBid.Suit != bid.Suit)
            return false;

        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Suit == null)
            return null;

        var nextLevel = GetNextSuitBidLevel((Suit)bid.Suit, ctx.AuctionEvaluation.CurrentContract);
        bool isMajor = bid.Suit == Suit.Hearts || bid.Suit == Suit.Spades;

        var constraints = new CompositeConstraint();
        // Raised suit (partner's suit) — this is the primary constraint shown
        constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 5));
        // Opening suit: 4-5 cards (6+ would trigger RebidOwnSuit)
        constraints.Add(new SuitLengthConstraint(ctx.AuctionEvaluation.OpeningBid!.Suit, 4, 5));

        if (isMajor && bid.Level == 4)
        {
            // Game raise: 19+ HCP
            constraints.Add(new HcpConstraint(19, 30));
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        if (bid.Level > nextLevel)
        {
            // Invitational jump raise: 16-18 HCP
            constraints.Add(new HcpConstraint(16, 18));
            return new BidInformation(bid, constraints, PartnershipBiddingState.GameInvitational);
        }

        // Minimum raise: 12-15 HCP
        constraints.Add(new HcpConstraint(12, 15));
        return new BidInformation(bid, constraints, PartnershipBiddingState.FitEstablished);
    }
}
