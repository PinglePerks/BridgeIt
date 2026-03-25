using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's rebid after opener rebids 1NT (15-17 balanced).
///
/// Sequence: 1x – 1y – 1NT – ?
///
/// Opener has shown 15-17 HCP, balanced. Responder now places the contract:
///
///   13+ HCP, 5+ card major  → 4M  (game in major, known 5-3 or better fit)
///   13+ HCP                 → 3NT (game in NT)
///   10-12 HCP, 5+ major     → 3M  (invite with suit — opener picks 3NT or 4M)
///   10-12 HCP               → 2NT (invite)
///   6-9 HCP, 5+ own suit    → 2 of own suit (sign-off, weakness)
///   6-9 HCP, 3+ opener maj  → 2 of opener's major (preference sign-off)
///   6-9 HCP                 → Pass
///
/// Priority 50 — above generic rebid rules, below convention-specific rebids.
/// </summary>
public class AcolResponderAfterOpener1NTRebid : BiddingRuleBase
{
    public override string Name => "Acol responder after opener 1NT rebid";
    public override int Priority => 50;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 2)
            return false;
        if (auction.OpeningBid?.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested)
            return false;

        // My last bid must have been a suit (new suit response)
        var myBid = auction.MyLastNonPassBid;
        if (myBid == null || myBid.Type != BidType.Suit)
            return false;

        // Partner (opener) must have rebid 1NT
        return auction.PartnerLastNonPassBid == Bid.NoTrumpsBid(1);
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        bool myIsMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;
        var mySuitLength = ctx.HandEvaluation.Shape[mySuit];

        // ── Game values (13+) ──
        if (hcp >= 13)
        {
            // 5+ card major → game in major
            if (myIsMajor && mySuitLength >= 5)
                return Bid.SuitBid(4, mySuit);

            return Bid.NoTrumpsBid(3);
        }

        // ── Invitational (10-12) ──
        if (hcp >= 10)
        {
            // 5+ card major → invite in that suit
            if (myIsMajor && mySuitLength >= 5)
                return Bid.SuitBid(3, mySuit);

            return Bid.NoTrumpsBid(2);
        }

        // ── Weak (6-9) ──

        // 5+ card suit → sign off at 2-level in own suit
        if (mySuitLength >= 5)
        {
            var level = GetNextSuitBidLevel(mySuit, ctx.AuctionEvaluation.CurrentContract);
            if (level == 2)
                return Bid.SuitBid(2, mySuit);
        }

        // 3+ cards in opener's major → preference
        bool openerIsMajor = openerSuit == Suit.Hearts || openerSuit == Suit.Spades;
        if (openerIsMajor && ctx.HandEvaluation.Shape[openerSuit] >= 3)
        {
            var level = GetNextSuitBidLevel(openerSuit, ctx.AuctionEvaluation.CurrentContract);
            if (level == 2)
                return Bid.SuitBid(2, openerSuit);
        }

        return Bid.Pass();
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;
        if (bid.Type == BidType.NoTrumps && (bid.Level == 2 || bid.Level == 3)) return true;

        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;

        if (bid.Type == BidType.Suit)
        {
            // Rebid own suit at 2 (weak) or 3 (invite) or 4 (game)
            if (bid.Suit == mySuit && bid.Level >= 2 && bid.Level <= 4)
                return true;
            // Preference to opener's suit at 2-level
            if (bid.Suit == openerSuit && bid.Level == 2)
                return true;
        }

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;

        // Pass = 6-9 HCP
        if (bid.Type == BidType.Pass)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(6, 9) } },
                PartnershipBiddingState.SignOff);

        // 3NT = 13+ HCP
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(13, 30) } },
                PartnershipBiddingState.SignOff);

        // 2NT = 10-12 HCP invite
        if (bid.Type == BidType.NoTrumps && bid.Level == 2)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(10, 12) } },
                PartnershipBiddingState.GameInvitational);

        if (bid.Type == BidType.Suit && bid.Suit == mySuit)
        {
            // 4M = game with 5+ card major
            if (bid.Level == 4)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(13, 30), new SuitLengthConstraint(mySuit, 5, 10) } },
                    PartnershipBiddingState.SignOff);

            // 3M = invite with 5+ card major
            if (bid.Level == 3)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(10, 12), new SuitLengthConstraint(mySuit, 5, 10) } },
                    PartnershipBiddingState.GameInvitational);

            // 2M = weak sign-off with 5+ cards
            if (bid.Level == 2)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(6, 9), new SuitLengthConstraint(mySuit, 5, 10) } },
                    PartnershipBiddingState.SignOff);
        }

        // Preference to opener's suit
        if (bid.Type == BidType.Suit && bid.Suit == openerSuit && bid.Level == 2)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(6, 9), new SuitLengthConstraint(openerSuit, 3, 5) } },
                PartnershipBiddingState.SignOff);

        return null;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(10, 30) } };
}
