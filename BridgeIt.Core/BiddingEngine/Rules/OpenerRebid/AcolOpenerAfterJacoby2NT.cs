using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

/// <summary>
/// Opener's rebid after partner responds 2NT (Jacoby) to a 1-major opening.
/// The auction is already game-forcing; opener now describes hand type:
///
///   4M       — minimum opening (12–14 HCP), no notable shape feature. Sign-off.
///   3-of-side — singleton or void in the bid suit (any strength).
///   4-of-side — five-card second suit, below 4M (any strength).
///   3NT      — strong balanced hand (18–19 HCP), no shortness.
///   3M       — six-or-more-card trump suit with extra values (15–17 HCP).
///
/// Priority 60 — fires before generic 1-suit rebid rules (25–45).
/// </summary>
public class AcolOpenerAfterJacoby2NT : BiddingRuleBase
{
    public override string Name => "Acol opener rebid after Jacoby 2NT";
    public override int Priority => 60;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;
        if (auction.OpeningBid?.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        var suit = auction.OpeningBid.Suit;
        if (suit != Suit.Hearts && suit != Suit.Spades)
            return false;
        return auction.PartnerLastNonPassBid == Bid.NoTrumpsBid(2);
    }

    // All hands are handled — no hand filter needed.
    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    // ── Forward (choosing the bid) ──────────────────────────────────────────

    public override Bid? Apply(DecisionContext ctx)
    {
        var trump = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var hcp   = ctx.HandEvaluation.Hcp;
        var shape = ctx.HandEvaluation.Shape;

        // 1. Strong balanced — no shortness, 18-19 HCP.
        if (hcp >= 18 && ctx.HandEvaluation.IsBalanced)
            return Bid.NoTrumpsBid(3);

        // 2. Singleton or void in a side suit — bid 3 of the cheapest short suit.
        var shortSuit = FindShortSuit(shape, trump);
        if (shortSuit.HasValue)
            return Bid.SuitBid(3, shortSuit.Value);

        // 3. Five-card side suit that fits below game — bid 4 of that suit.
        var secondSuit = FindSecondSuit(shape, trump);
        if (secondSuit.HasValue)
            return Bid.SuitBid(4, secondSuit.Value);

        // 4. Six-card trump suit with extra values.
        if (shape[trump] >= 6 && hcp >= 15)
            return Bid.SuitBid(3, trump);

        // 5. Minimum hand — sign off at game in the major.
        return Bid.SuitBid(4, trump);
    }

    // ── Backward (explaining an observed bid) ──────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        var trump = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;

        // 4M — minimum sign-off
        if (bid.Type == BidType.Suit && bid.Suit == trump && bid.Level == 4)
            return true;

        // 3M — extra trump length
        if (bid.Type == BidType.Suit && bid.Suit == trump && bid.Level == 3)
            return true;

        // 3NT — strong balanced
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            return true;

        // 3-of-side-suit — shortness
        if (bid.Type == BidType.Suit && bid.Suit != trump && bid.Level == 3)
            return true;

        // 4-of-side-suit — five-card second suit (must rank below trump to stay below 4M)
        if (bid.Type == BidType.Suit && bid.Suit != trump && bid.Level == 4
            && (int)bid.Suit!.Value < (int)trump)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var trump = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;

        // 4M — minimum opening, no notable feature
        if (bid.Type == BidType.Suit && bid.Suit == trump && bid.Level == 4)
        {
            var c = new CompositeConstraint();
            c.Add(new HcpConstraint(12, 14));
            c.Add(new SuitLengthConstraint(trump, 5, 6));
            return new BidInformation(bid, c, PartnershipBiddingState.SignOff);
        }

        // 3M — extra trump length with values
        if (bid.Type == BidType.Suit && bid.Suit == trump && bid.Level == 3)
        {
            var c = new CompositeConstraint();
            c.Add(new HcpConstraint(15, 17));
            c.Add(new SuitLengthConstraint(trump, 6, 10));
            return new BidInformation(bid, c, PartnershipBiddingState.SlamExploration);
        }

        // 3NT — strong balanced
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
        {
            var c = new CompositeConstraint();
            c.Add(new HcpConstraint(18, 19));
            c.Add(new BalancedConstraint());
            return new BidInformation(bid, c, PartnershipBiddingState.SlamExploration);
        }

        // 3-of-side-suit — singleton or void in the bid suit
        if (bid.Type == BidType.Suit && bid.Suit != trump && bid.Level == 3)
        {
            var c = new CompositeConstraint();
            c.Add(new SuitLengthConstraint(bid.Suit, 0, 1));
            c.Add(new SuitLengthConstraint(trump, 5, 10));
            return new BidInformation(bid, c, PartnershipBiddingState.FitEstablished);
        }

        // 4-of-side-suit — five-card second suit
        if (bid.Type == BidType.Suit && bid.Suit != trump && bid.Level == 4
            && (int)bid.Suit!.Value < (int)trump)
        {
            var c = new CompositeConstraint();
            c.Add(new SuitLengthConstraint(bid.Suit, 5, 10));
            c.Add(new SuitLengthConstraint(trump, 5, 10));
            return new BidInformation(bid, c, PartnershipBiddingState.SlamExploration);
        }

        return null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the lowest-ranked side suit with a singleton or void, or null if none.
    /// Showing the cheapest short suit keeps the auction low.
    /// </summary>
    private static Suit? FindShortSuit(Dictionary<Suit, int> shape, Suit trump)
        => Enum.GetValues<Suit>()
               .Where(s => s != trump && shape.GetValueOrDefault(s, 0) <= 1)
               .Cast<Suit?>()
               .FirstOrDefault();

    /// <summary>
    /// Returns the longest 5+ card side suit that ranks below the trump suit,
    /// ensuring the bid (4-of-suit) stays below game in the major.
    /// When two suits have equal length the higher-ranked one is preferred.
    /// </summary>
    private static Suit? FindSecondSuit(Dictionary<Suit, int> shape, Suit trump)
        => shape
            .Where(kv => kv.Key != trump
                      && kv.Value >= 5
                      && (int)kv.Key < (int)trump)
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => (int)kv.Key)
            .Select(kv => (Suit?)kv.Key)
            .FirstOrDefault();
}
