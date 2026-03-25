using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: rebid a previously-shown suit when partner's picture of our
/// hand shape is incomplete.
///
/// Compares actual hand (HandEvaluation.Shape) against what we've communicated
/// (TableKnowledge.Me.MinShape). If we have more cards in a suit than partner
/// thinks, rebid it to correct their picture.
///
/// Example: With 5♠ 5♥, after 1♠ – (response) – 2♥, partner infers 4+ hearts.
/// This rule rebids 3♥ to show the fifth heart.
///
/// Fires for either opener or responder at any round. Priority 4 — above
/// game/invite/sign-off knowledge rules, so shape correction happens before
/// the final contract decision.
/// </summary>
public class KnowledgeShapeCorrection : BiddingRuleBase
{
    public override string Name => "Knowledge: Shape correction";
    public override int Priority { get; } = 4;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // Only act when partner has actively communicated via a bid
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == null) return false;

        return FindCorrectionSuit(ctx) != null;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = FindCorrectionSuit(ctx);
        if (suit == null) return null;

        var level = GetNextSuitBidLevel(suit.Value, ctx.AuctionEvaluation.CurrentContract);
        return Bid.SuitBid(level, suit.Value);
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid is { Type: BidType.Suit, Suit: not null } && bid.Level >= 2 && bid.Level <= 4;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid is not { Type: BidType.Suit, Suit: not null })
            return null;

        // Rebidding a suit shows at least one more card than previously known
        var currentMin = ctx.TableKnowledge.Me.MinShape[bid.Suit.Value];
        var inferredMin = Math.Max(currentMin + 1, 5); // At least 5 to rebid

        var constraints = new CompositeConstraint
        {
            Constraints = { new SuitLengthConstraint(bid.Suit.Value, inferredMin, 13) }
        };

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    // No meaningful forward constraint — this is a general-purpose catch-all
    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction) => null;

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds the best suit to correct shape in. Returns null if no correction needed.
    /// Prefers suits with the most hidden length, then highest ranking.
    /// </summary>
    private static Suit? FindCorrectionSuit(DecisionContext ctx)
    {
        Suit? best = null;
        var bestExtra = 0;

        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            var actual = ctx.HandEvaluation.Shape[suit];
            var shown = ctx.TableKnowledge.Me.MinShape[suit];

            if (shown == 0) continue;       // Never bid this suit — nothing to correct
            if (actual <= shown) continue;   // No hidden length
            if (actual < 5) continue;        // Not worth correcting short suits

            // Don't correct if rebid would push to or past game level
            var contract = ctx.AuctionEvaluation.CurrentContract;
            var nextLevel = Bid.NextLevelForSuit(suit, contract);
            var gameLevel = IsMajor(suit) ? 4 : 5;
            if (nextLevel >= gameLevel) continue;

            var extra = actual - shown;
            if (extra > bestExtra || (extra == bestExtra && (int)suit > (int)(best ?? (Suit)(-1))))
            {
                best = suit;
                bestExtra = extra;
            }
        }

        return best;
    }

    private static bool IsMajor(Suit suit) => suit == Suit.Hearts || suit == Suit.Spades;
}
