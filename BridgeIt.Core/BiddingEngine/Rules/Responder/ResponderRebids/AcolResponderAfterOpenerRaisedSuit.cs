using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's rebid after opener raised responder's suit.
///
/// Sequences: 1x – 1y – 2y – ?   (simple raise, 12-15 HCP, 4+ support)
///            1x – 1y – 3y – ?   (jump raise, 16-18 HCP, 4+ support)
///            1x – 1y – 4M – ?   (game raise, 19+ HCP)
///
/// Fit is established — responder decides the level:
///   After simple raise (2y):
///     Pass      — 6-9 HCP (sign off)
///     3y        — 10-12 HCP (invite)
///     4M / 5m   — 13+ HCP (game)
///
///   After jump raise (3y):
///     Use GetLevelVerdict — pass or bid game.
///
///   After game raise (4M):
///     Pass — game already reached.
///
/// Priority 52 — above generic responder rebids, below Jacoby 2NT follow-ups.
/// </summary>
public class AcolResponderAfterOpenerRaisedSuit : BiddingRuleBase
{
    public override string Name => "Acol responder after opener raised suit";
    public override int Priority => 52;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 2)
            return false;
        if (auction.OpeningBid?.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested)
            return false;

        // My last bid must have been a suit (the suit opener raised)
        var myBid = auction.MyLastNonPassBid;
        if (myBid == null || myBid.Type != BidType.Suit)
            return false;

        // Partner (opener) must have raised MY suit
        var partnerBid = auction.PartnerLastNonPassBid;
        if (partnerBid == null || partnerBid.Type != BidType.Suit)
            return false;
        if (partnerBid.Suit != myBid.Suit)
            return false;

        // Partner's raise must be at a higher level than my original bid
        if (partnerBid.Level <= myBid.Level)
            return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var partnerLevel = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Level;
        bool isMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;
        var gameLevel = isMajor ? 4 : 5;

        // Game already reached — pass
        if (partnerLevel >= gameLevel)
            return Bid.Pass();

        var threshold = isMajor ? 25 : 29;
        var verdict = ctx.GetLevelVerdict(threshold);

        if (verdict == LevelVerdict.BidGame)
            return Bid.SuitBid(gameLevel, mySuit);

        if (verdict == LevelVerdict.Invite && partnerLevel < gameLevel - 1)
            return Bid.SuitBid(partnerLevel + 1, mySuit);

        return Bid.Pass();
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;

        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        if (bid.Type == BidType.Suit && bid.Suit == mySuit)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var partnerLevel = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Level;

        if (bid.Type == BidType.Pass)
        {
            // After simple raise (2y): 6-9 HCP. After jump raise (3y): weak end.
            var maxHcp = partnerLevel == 2 ? 9 : 12;
            var c = new CompositeConstraint();
            c.Add(new HcpConstraint(6, maxHcp));
            return new BidInformation(bid, c, PartnershipBiddingState.SignOff);
        }

        if (bid.Type == BidType.Suit)
        {
            var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
            bool isMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;
            var gameLevel = isMajor ? 4 : 5;

            if (bid.Level == gameLevel)
            {
                var c = new CompositeConstraint();
                c.Add(new HcpConstraint(13, 30));
                return new BidInformation(bid, c, PartnershipBiddingState.SignOff);
            }

            // Invite raise (e.g. 3y after 2y)
            var c2 = new CompositeConstraint();
            c2.Add(new HcpConstraint(10, 12));
            return new BidInformation(bid, c2, PartnershipBiddingState.GameInvitational);
        }

        return null;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(10, 30) } };
}
