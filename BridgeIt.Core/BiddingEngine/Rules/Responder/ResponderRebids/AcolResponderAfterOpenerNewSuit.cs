using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's rebid after opener bids a new suit (showing a 4+ card second suit).
///
/// Sequence: 1x – 1y – 2z – ?   (opener showed two suits)
///
/// Responder must choose between:
///   Preference    — return to opener's first suit (cheaper = preferred)
///   Own suit      — rebid 6+ card suit
///   NT            — balanced with stoppers
///   Raise         — support for opener's second suit
///
/// Strength tiers:
///   6-9 HCP  — simple preference, pass, or rebid own suit (sign-off)
///   10-12    — 2NT invite, jump preference to 3 of opener's suit
///   13+      — 3NT, 4M game, or new suit (game-forcing)
///
/// Priority 45 — below own-suit rebid (48), above knowledge catch-alls.
/// </summary>
public class AcolResponderAfterOpenerNewSuit : BiddingRuleBase
{
    public override string Name => "Acol responder after opener new suit";
    public override int Priority => 45;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 2)
            return false;
        if (auction.OpeningBid?.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested)
            return false;

        // My last bid must have been a suit
        var myBid = auction.MyLastNonPassBid;
        if (myBid == null || myBid.Type != BidType.Suit)
            return false;

        // Partner (opener) must have bid a NEW suit — different from both opening suit and my suit
        var partnerBid = auction.PartnerLastNonPassBid;
        if (partnerBid == null || partnerBid.Type != BidType.Suit)
            return false;
        if (partnerBid.Suit == auction.OpeningBid.Suit)
            return false;
        if (partnerBid.Suit == myBid.Suit)
            return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var openerFirstSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var openerSecondSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var shape = ctx.HandEvaluation.Shape;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        bool openerFirstIsMajor = openerFirstSuit == Suit.Hearts || openerFirstSuit == Suit.Spades;
        bool myIsMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;

        // ── Game values (13+) ──
        if (hcp >= 13)
        {
            // 4+ support for opener's major → game
            if (openerFirstIsMajor && shape[openerFirstSuit] >= 3)
                return Bid.SuitBid(4, openerFirstSuit);
            if (myIsMajor && shape[mySuit] >= 6)
                return Bid.SuitBid(4, mySuit);
            return Bid.NoTrumpsBid(3);
        }

        // ── Invitational (10-12) ──
        if (hcp >= 10)
        {
            // 3+ support for opener's first suit → jump preference (invite)
            if (shape[openerFirstSuit] >= 3)
                return Bid.SuitBid(3, openerFirstSuit);
            return Bid.NoTrumpsBid(2);
        }

        // ── Weak (6-9) ──

        // 3+ cards in opener's first suit → preference (return to first suit)
        // Preferred over passing in second suit because opener's first suit is
        // typically longer (5+) giving a better fit, and majors play better.
        if (shape[openerFirstSuit] >= 3)
        {
            var level = GetNextSuitBidLevel(openerFirstSuit, contract);
            if (level <= 2)
                return Bid.SuitBid(level, openerFirstSuit);
        }

        // 6+ in own suit → rebid own suit
        if (shape[mySuit] >= 6)
        {
            var level = GetNextSuitBidLevel(mySuit, contract);
            if (level <= 2)
                return Bid.SuitBid(level, mySuit);
        }

        // No preference possible — pass (plays in opener's second suit)
        return Bid.Pass();
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;
        if (bid.Type == BidType.NoTrumps && (bid.Level == 2 || bid.Level == 3)) return true;

        var openerFirstSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;

        if (bid.Type == BidType.Suit)
        {
            // Preference to opener's first suit
            if (bid.Suit == openerFirstSuit && bid.Level >= 2 && bid.Level <= 4) return true;
            // Rebid own suit
            if (bid.Suit == mySuit && bid.Level >= 2 && bid.Level <= 4) return true;
        }

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var openerFirstSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;

        if (bid.Type == BidType.Pass)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(6, 9) } },
                PartnershipBiddingState.SignOff);

        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(13, 30) } },
                PartnershipBiddingState.SignOff);

        if (bid.Type == BidType.NoTrumps && bid.Level == 2)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(10, 12) } },
                PartnershipBiddingState.GameInvitational);

        if (bid.Type == BidType.Suit && bid.Suit == openerFirstSuit)
        {
            bool isMajor = openerFirstSuit == Suit.Hearts || openerFirstSuit == Suit.Spades;
            var gameLevel = isMajor ? 4 : 5;

            // Game in opener's suit
            if (bid.Level == gameLevel)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(13, 30), new SuitLengthConstraint(openerFirstSuit, 3, 5) } },
                    PartnershipBiddingState.SignOff);

            // Jump preference (invite)
            if (bid.Level == 3)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(10, 12), new SuitLengthConstraint(openerFirstSuit, 3, 5) } },
                    PartnershipBiddingState.GameInvitational);

            // Simple preference (weak)
            if (bid.Level == 2)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(6, 9), new SuitLengthConstraint(openerFirstSuit, 3, 5) } },
                    PartnershipBiddingState.SignOff);
        }

        if (bid.Type == BidType.Suit && bid.Suit == mySuit && bid.Level == 2)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(6, 9), new SuitLengthConstraint(mySuit, 6, 10) } },
                PartnershipBiddingState.SignOff);

        return null;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(10, 30) } };
}
