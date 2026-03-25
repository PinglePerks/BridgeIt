using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolOpenerAfterMajorRaise : BiddingRuleBase
{
    public override string Name { get; } = "After major raise";
    public override int Priority { get; }

    public AcolOpenerAfterMajorRaise(int priority = 45)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;

        // Opening must be a major suit
        if (auction.OpeningBid?.Type != BidType.Suit)
            return false;

        var openingSuit = auction.OpeningBid.Suit!.Value;
        if (openingSuit != Suit.Hearts && openingSuit != Suit.Spades)
            return false;

        // Partner must have raised our major (use PartnerLastNonPassBid to skip opponents' passes)
        var partnerBid = auction.PartnerLastNonPassBid;
        if (partnerBid == null) return false;
        if (partnerBid.Type != BidType.Suit) return false;
        if (partnerBid.Suit != openingSuit) return false;
        if (partnerBid.Level < 2 || partnerBid.Level > 3) return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var suit = (Suit)ctx.AuctionEvaluation.OpeningBid!.Suit!;
        var partnerLevel = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Level;

        if (partnerLevel == 2)
        {
            // Partner showed 6-9: Pass 12-14, Invite 15-16, Game 17+
            if (hcp >= 17) return Bid.SuitBid(4, suit);
            if (hcp >= 15) return Bid.SuitBid(3, suit);
            return Bid.Pass();
        }

        // Partner showed 10-12 (limit raise to 3M): Pass 12-14, Game 15+
        if (hcp >= 15) return Bid.SuitBid(4, suit);
        return Bid.Pass();
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        var openingSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var partnerLevel = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Level;

        // Pass is always explainable
        if (bid.Type == BidType.Pass)
            return true;

        // Must be the same suit as opening
        if (bid.Type != BidType.Suit || bid.Suit != openingSuit)
            return false;

        // After 2M: can bid 3M (invite) or 4M (game)
        if (partnerLevel == 2 && (bid.Level == 3 || bid.Level == 4))
            return true;

        // After 3M: can only bid 4M (game)
        if (partnerLevel == 3 && bid.Level == 4)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();

        if (bid.Type == BidType.Pass)
        {
            constraints.Add(new HcpConstraint(12, 14));
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        if (bid.Type == BidType.Suit)
        {
            var openingSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
            if (bid.Suit == openingSuit && bid.Level == 3)
            {
                // Invite: 15-16
                constraints.Add(new HcpConstraint(15, 16));
                return new BidInformation(bid, constraints, PartnershipBiddingState.GameInvitational);
            }

            if (bid.Suit == openingSuit && bid.Level == 4)
            {
                // Game: 17+
                constraints.Add(new HcpConstraint(17, 30));
                return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
            }
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
    }
}
