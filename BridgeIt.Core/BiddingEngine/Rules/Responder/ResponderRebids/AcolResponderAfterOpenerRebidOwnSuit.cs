using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's rebid after opener rebids their own suit (showing 6+ cards).
///
/// Sequences: 1x – 1y – 2x – ?   (simple rebid, 12-15 HCP, 6+ cards)
///            1x – 1y – 3x – ?   (jump rebid, 16-19 HCP, 6+ cards)
///
/// After simple rebid (2x, opener 12-15):
///   Pass             — 6-9 HCP, no fit (sign off)
///   2 of own suit    — 6-9 HCP, 6+ cards in own suit (sign-off preference)
///   3x (raise)       — 10-12 HCP, 3+ support (invite with fit)
///   2NT              — 10-12 HCP, no fit (invite in NT)
///   4M / 3NT         — 13+ HCP (game)
///
/// After jump rebid (3x, opener 16-19):
///   Combined verdict determines pass vs game.
///
/// Priority 48 — below raised-suit (52), above new-suit (45).
/// </summary>
public class AcolResponderAfterOpenerRebidOwnSuit : BiddingRuleBase
{
    public override string Name => "Acol responder after opener rebid own suit";
    public override int Priority { get; }

    public AcolResponderAfterOpenerRebidOwnSuit(int priority = 48)
    {
        Priority = priority;
    }

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

        // Partner (opener) must have rebid their OPENING suit at a higher level
        var partnerBid = auction.PartnerLastNonPassBid;
        if (partnerBid == null || partnerBid.Type != BidType.Suit)
            return false;
        if (partnerBid.Suit != auction.OpeningBid.Suit)
            return false;
        if (partnerBid.Level <= 1)
            return false;

        // Must NOT be a raise of MY suit (that's handled by RaisedSuit rule)
        if (partnerBid.Suit == myBid.Suit)
            return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        var partnerLevel = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Level;
        bool openerIsMajor = openerSuit == Suit.Hearts || openerSuit == Suit.Spades;
        bool myIsMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;
        var mySuitLength = ctx.HandEvaluation.Shape[mySuit];
        var openerSuitLength = ctx.HandEvaluation.Shape[openerSuit];

        // After jump rebid (3x), use combined HCP verdict
        if (partnerLevel >= 3)
        {
            var threshold = openerIsMajor ? 25 : 29;
            var verdict = ctx.GetLevelVerdict(threshold);

            if (verdict == LevelVerdict.BidGame)
            {
                // With 3+ support → game in opener's suit
                if (openerSuitLength >= 3)
                {
                    var gameLevel = openerIsMajor ? 4 : 5;
                    return Bid.SuitBid(gameLevel, openerSuit);
                }
                return Bid.NoTrumpsBid(3);
            }

            return Bid.Pass();
        }

        // ── After simple rebid (2x, opener 12-15) ──

        // Game values (13+)
        if (hcp >= 13)
        {
            if (openerSuitLength >= 3 && openerIsMajor)
                return Bid.SuitBid(4, openerSuit);
            if (myIsMajor && mySuitLength >= 6)
                return Bid.SuitBid(4, mySuit);
            return Bid.NoTrumpsBid(3);
        }

        // Invitational (10-12)
        if (hcp >= 10)
        {
            // 3+ support for opener → invite in that suit
            if (openerSuitLength >= 3)
                return Bid.SuitBid(3, openerSuit);
            return Bid.NoTrumpsBid(2);
        }

        // Weak (6-9)

        // 6+ in own suit → sign off at 2-level if affordable
        if (mySuitLength >= 6)
        {
            var level = GetNextSuitBidLevel(mySuit, ctx.AuctionEvaluation.CurrentContract);
            if (level == 2)
                return Bid.SuitBid(2, mySuit);
        }

        return Bid.Pass();
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;
        if (bid.Type == BidType.NoTrumps && (bid.Level == 2 || bid.Level == 3)) return true;

        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;

        if (bid.Type == BidType.Suit)
        {
            if (bid.Suit == openerSuit && bid.Level >= 3 && bid.Level <= 5) return true;
            if (bid.Suit == mySuit && bid.Level >= 2 && bid.Level <= 4) return true;
        }

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var openerSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
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

        if (bid.Type == BidType.Suit && bid.Suit == openerSuit)
        {
            bool isMajor = openerSuit == Suit.Hearts || openerSuit == Suit.Spades;
            var gameLevel = isMajor ? 4 : 5;

            if (bid.Level == gameLevel)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(13, 30), new SuitLengthConstraint(openerSuit, 3, 5) } },
                    PartnershipBiddingState.SignOff);

            if (bid.Level == 3)
                return new BidInformation(bid,
                    new CompositeConstraint { Constraints = { new HcpConstraint(10, 12), new SuitLengthConstraint(openerSuit, 3, 5) } },
                    PartnershipBiddingState.GameInvitational);
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
