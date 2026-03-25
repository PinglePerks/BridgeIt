using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's rebid after opener rebids 2NT (18-19 balanced).
///
/// Sequence: 1x – 1y – 2NT – ?
///
/// Opener has shown 18-19 HCP, balanced. Responder places the contract:
///
///   8+ HCP, 5+ card major  → 4M  (game in major)
///   8+ HCP                 → 3NT (game in NT)
///   6-7 HCP                → Pass (combined 24-26, borderline)
///
/// Priority 50 — same level as other responder rebid rules.
/// </summary>
public class AcolResponderAfterOpener2NTRebid : BiddingRuleBase
{
    public override string Name => "Acol responder after opener 2NT rebid";
    public override int Priority => 50;

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

        // Partner (opener) must have rebid 2NT
        return auction.PartnerLastNonPassBid == Bid.NoTrumpsBid(2);
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        bool myIsMajor = mySuit == Suit.Hearts || mySuit == Suit.Spades;
        var mySuitLength = ctx.HandEvaluation.Shape[mySuit];

        // Combined minimum: hcp + 18. Game if combined >= 25 → hcp >= 7.
        // But with 6-7 it's borderline, so use GetLevelVerdict.
        var verdict = ctx.GetLevelVerdict(25);

        if (verdict == LevelVerdict.BidGame || verdict == LevelVerdict.Invite)
        {
            // 5+ card major → game in major
            if (myIsMajor && mySuitLength >= 5)
                return Bid.SuitBid(4, mySuit);

            return Bid.NoTrumpsBid(3);
        }

        // Sign off — very weak responder (6 HCP, combined max < 25)
        return Bid.Pass();
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;
        if (bid.Type == BidType.NoTrumps && bid.Level == 3) return true;

        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;
        if (bid.Type == BidType.Suit && bid.Suit == mySuit && bid.Level == 4) return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var mySuit = ctx.AuctionEvaluation.MyLastNonPassBid!.Suit!.Value;

        if (bid.Type == BidType.Pass)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(6, 7) } },
                PartnershipBiddingState.SignOff);

        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(7, 30) } },
                PartnershipBiddingState.SignOff);

        if (bid.Type == BidType.Suit && bid.Suit == mySuit && bid.Level == 4)
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new HcpConstraint(7, 30), new SuitLengthConstraint(mySuit, 5, 10) } },
                PartnershipBiddingState.SignOff);

        return null;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(7, 30) } };
}
