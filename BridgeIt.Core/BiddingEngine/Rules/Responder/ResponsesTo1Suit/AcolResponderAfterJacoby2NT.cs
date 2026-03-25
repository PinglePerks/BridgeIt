using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

/// <summary>
/// Responder's continuation after opener's Jacoby 2NT rebid.
///
/// After 1M – 2NT – [opener's rebid], this rule handles the default game
/// sign-off.  Slam-seeking bids (cuebids, RKCB) live in higher-priority
/// rules and are tried before this one.
///
///   Pass  — if opener already signed off at 4M (game reached).
///   4M    — otherwise, close the auction at game in the major.
///
/// Hand evaluation:
///   - Sign off immediately when combined HCP cannot reach slam (max &lt; 33).
///   - Slam-hunting hands are handled by higher-priority rules; this rule acts
///     as the safety net.
///
/// Priority 55 — below slam-investigation rules, above generic fallbacks.
/// </summary>
public class AcolResponderAfterJacoby2NT : BiddingRuleBase
{
    public override string Name => "Acol responder sign-off after Jacoby 2NT rebid";
    public override int Priority => 55;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 2)
            return false;
        if (auction.OpeningBid?.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        var suit = auction.OpeningBid.Suit;
        if (suit != Suit.Hearts && suit != Suit.Spades)
            return false;
        // My previous bid was the Jacoby 2NT.
        return auction.MyLastNonPassBid == Bid.NoTrumpsBid(2);
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // Sign off when slam is out of range.
        // Slam exploration rules (higher priority) will preempt this when
        // combined values make slam possible.
        return ctx.CombinedHcpMax < 33;
    }

    // ── Forward ─────────────────────────────────────────────────────────────

    public override Bid? Apply(DecisionContext ctx)
    {
        var trump = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        // Opener bid 4M (minimum sign-off) — game is already reached.
        if (contract?.Type == BidType.Suit
            && contract.Suit == trump
            && contract.Level == 4)
            return Bid.Pass();

        return Bid.SuitBid(4, trump);
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass) return true;

        var trump = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        return bid.Type == BidType.Suit && bid.Suit == trump && bid.Level == 4;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        // Responder is signing off: still holds 13+ (shown by Jacoby) but
        // combined values don't reach slam threshold.
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(13, 32));
        return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
    }
}
